using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using static WomPlatform.Connector.Models.MerchantLoginResultV2;

namespace WomPlatform.Web.Api.Service {
    public class OfferService : BaseService {

        public OfferService(
            MongoClient client,
            IConfiguration configuration,
            ILogger<BackupService> logger
        ) : base(client, logger) {
        }

        private IMongoCollection<Offer> OfferCollection {
            get {
                return MainDatabase.GetCollection<Offer>("Offers");
            }
        }

        public Task AddOffer(Offer offer) {
            return OfferCollection.InsertOneAsync(offer);
        }

        public class GroupedOffersByPos {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement("name")]
            public string Name { get; set; }

            [BsonElement("description")]
            public string Description { get; set; }

            [BsonElement("position")]
            public GeoJsonPoint<GeoJson2DGeographicCoordinates> Position { get; set; }

            [BsonElement("distance")]
            public double Distance { get; set; }

            [BsonElement("offerCount")]
            public int OfferCount { get; set; }

            public class Offer {
                [BsonId]
                public Guid Otc { get; set; }

                [BsonElement("title")]
                public string Title { get; set; }

                [BsonElement("description")]
                public string Description { get; set; }

                [BsonElement("cost")]
                public int Cost { get; set; }

                [BsonElement("filter")]
                public Filter Filter { get; set; }

                [BsonElement("createdOn")]
                [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
                public DateTime CreatedOn { get; set; }

                [BsonElement("lastUpdate")]
                [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
                public DateTime LastUpdate { get; set; }
            }

            [BsonElement("offers")]
            public Offer[] Offers { get; set; }
        }

        public Task<List<GroupedOffersByPos>> GetOffersByDistance(double latitude, double longitude, double rangeKms) {
            var pipeline = new EmptyPipelineDefinition<Offer>()
                .AppendStage<Offer, Offer, Offer>(BsonDocument.Parse(string.Format(CultureInfo.InvariantCulture, @"{{
                    $geoNear: {{
                        near: {{ ""type"": ""Point"", ""coordinates"": [ {0}, {1} ] }},
                        distanceField: 'distance',
                        maxDistance: {2},
                        spherical: true,
                    }}
                }}", longitude, latitude, rangeKms * 1000.0)))
                .Match(Builders<Offer>.Filter.Ne(o => o.Deactivated, true))
                .AppendStage<Offer, Offer, GroupedOffersByPos>(BsonDocument.Parse(@"{
                    $group: {
                        _id: ""$pos._id"",
                        name: { $first: ""$pos.name"" },
                        description: { $first: ""$pos.description"" },
                        position: { $first: ""$pos.position"" },
                        distance: { $first: ""$distance"" },
                        offerCount: { $sum: 1 },
                        offers: { $push: { _id: ""$_id"", title: ""$title"", description: ""$description"", cost: ""$cost"", filter: ""$filter"", createdOn: ""$createdOn"", lastUpdate: ""$lastUpdate"" } }
                    }
                }"))
                .Sort(Builders<GroupedOffersByPos>.Sort.Ascending(go => go.Distance))
            ;

            return OfferCollection.Aggregate(pipeline).ToListAsync();
        }

    }

}
