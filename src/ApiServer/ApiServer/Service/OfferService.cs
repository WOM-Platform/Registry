using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Web.Api.DatabaseDocumentModels;

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

        public enum OfferOrder {
            Distance,
            LastUpdate,
            AlfanumericTitle
        }

        private PipelineDefinition<Offer, GroupedOffersByPos> ApplyOrder(PipelineDefinition<Offer, GroupedOffersByPos> pipeline, OfferOrder orderBy) {
            switch(orderBy) {
                default:
                case OfferOrder.AlfanumericTitle:
                    return pipeline.Sort(Builders<GroupedOffersByPos>.Sort.Ascending(go => go.Name));

                case OfferOrder.Distance:
                    return pipeline.Sort(Builders<GroupedOffersByPos>.Sort.Ascending(go => go.Distance));

                case OfferOrder.LastUpdate:
                    return pipeline.Sort(Builders<GroupedOffersByPos>.Sort.Descending(go => go.MostRecentUpdate));
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

            [BsonElement("coverPath")]
            public string CoverPath { get; set; }

            [BsonElement("coverBlurHash")]
            public string CoverBlurHash { get; set; }

            [BsonElement("url")]
            public string Url { get; set; }

            [BsonElement("position")]
            public GeoJsonPoint<GeoJson2DGeographicCoordinates> Position { get; set; }

            [BsonElement("distance")]
            public double Distance { get; set; }

            [BsonElement("offerCount")]
            public int OfferCount { get; set; }

            [BsonElement("mostRecentUpdate")]
            public DateTime? MostRecentUpdate { get; set; }

            public class Offer {
                [BsonId]
                public ObjectId Id { get; set; }

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

        public Task<List<GroupedOffersByPos>> GetOffersByDistance(double latitude, double longitude, double rangeKms, OfferOrder orderBy) {
            var pipeline = new EmptyPipelineDefinition<Offer>()
                .AppendStage<Offer, Offer, Offer>(BsonDocument.Parse(string.Format(CultureInfo.InvariantCulture, @"{{
                    $geoNear: {{
                        key: ""pos.position"",
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
                        coverPath: { $first: ""$pos.coverPath"" },
                        coverBlurHash: { $first: ""$pos.coverBlurHash"" },
                        url: { $first: ""$pos.url"" },
                        position: { $first: ""$pos.position"" },
                        distance: { $first: ""$distance"" },
                        offerCount: { $sum: 1 },
                        mostRecentUpdate: { $max: ""$pos.lastUpdate"" },
                        offers: {
                            $push: {
                                _id: ""$_id"",
                                title: ""$title"",
                                description: ""$description"",
                                cost: ""$cost"",
                                filter: ""$filter"",
                                createdOn: ""$createdOn"",
                                lastUpdate: ""$lastUpdate""
                            }
                        }
                    }
                }"));

            pipeline = ApplyOrder(pipeline, orderBy);

            return OfferCollection.Aggregate(pipeline).ToListAsync();
        }

        public Task<List<GroupedOffersByPos>> GetOffersInBox(double lowerLeftLong, double lowerLeftLat, double upperRightLong, double upperRightLat) {
            var pipeline = new EmptyPipelineDefinition<Offer>()
                .Match(Builders<Offer>.Filter.Ne(o => o.Deactivated, true))
                .AppendStage<Offer, Offer, Offer>(BsonDocument.Parse(string.Format(CultureInfo.InvariantCulture, @"{{
                    $match: {{
                        ""pos.position"": {{
                            $geoWithin: {{
                                $box: [
                                    [{0}, {1}],
                                    [{2}, {3}]
                                ]
                            }}
                        }}
                    }}
                }}", lowerLeftLong, lowerLeftLat, upperRightLong, upperRightLat)))
                .AppendStage<Offer, Offer, GroupedOffersByPos>(BsonDocument.Parse(@"{
                    $group: {
                        _id: ""$pos._id"",
                        name: { $first: ""$pos.name"" },
                        description: { $first: ""$pos.description"" },
                        coverPath: { $first: ""$pos.coverPath"" },
                        coverBlurHash: { $first: ""$pos.coverBlurHash"" },
                        url: { $first: ""$pos.url"" },
                        position: { $first: ""$pos.position"" },
                        offerCount: { $sum: 1 },
                        mostRecentUpdate: { $max: ""$pos.lastUpdate"" },
                        offers: {
                            $push: {
                                _id: ""$_id"",
                                title: ""$title"",
                                description: ""$description"",
                                cost: ""$cost"",
                                filter: ""$filter"",
                                createdOn: ""$createdOn"",
                                lastUpdate: ""$lastUpdate""
                            }
                        }
                    }
                }"));

            return OfferCollection.Aggregate(pipeline).ToListAsync();
        }

        /// <summary>
        /// Sets cover path and blur hash on all offers by a given POS.
        /// </summary>
        public Task UpdatePosCovers(ObjectId posId, string coverPath, string coverBlurHash) {
            return OfferCollection.UpdateManyAsync(
                Builders<Offer>.Filter.Eq(o => o.Pos.Id, posId),
                Builders<Offer>.Update.Set(o => o.Pos.CoverPath, coverPath).Set(o => o.Pos.CoverBlurHash, coverBlurHash)
            );
        }

    }

}
