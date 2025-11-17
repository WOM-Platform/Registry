using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.DTO;
using WomPlatform.Web.Api.OutputModels.Offers;
using WomPlatform.Web.Api.Utilities;

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

        public Task ReplaceOffer(Offer offer) {
            var filter = Builders<Offer>.Filter.Eq(o => o.Id, offer.Id);
            return OfferCollection.ReplaceOneAsync(filter, offer);
        }

        public Task UpdateOfferDescription(ObjectId offerId, string title, string description) {
            return OfferCollection.UpdateOneAsync(
                Builders<Offer>.Filter.Eq(o => o.Id, offerId),
                Builders<Offer>.Update
                    .Set(o => o.Title, title)
                    .Set(o => o.Description, description)
            );
        }

        public Task DeactivateOffer(ObjectId offerId, bool deactivated) {
            return OfferCollection.UpdateOneAsync(
                Builders<Offer>.Filter.Eq(o => o.Id, offerId),
                Builders<Offer>.Update.Set(o => o.Deactivated, deactivated)
            );
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

                public class PaymentInformation {
                    [BsonElement("otc")]
                    public Guid Otc { get; set; }

                    [BsonElement("password")]
                    public string Password { get; set; }

                    [BsonElement("cost")]
                    public int Cost { get; set; }

                    [BsonElement("filter")]
                    [BsonIgnoreIfNull]
                    public Filter Filter { get; set; }
                }

                [BsonElement("payment")]
                public PaymentInformation Payment { get; set; }

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

        public Task<List<GroupedOffersByPos>> GetOffersWithDistance(double latitude, double longitude, double rangeKms, OfferOrder orderBy) {
            var pipeline = new EmptyPipelineDefinition<Offer>()
                // The geoNear stage automatically filters out offers without position
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
                                payment: ""$payment"",
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
                // The geoWithin match automatically filters out offers without position
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
                                payment: ""$payment"",
                                createdOn: ""$createdOn"",
                                lastUpdate: ""$lastUpdate""
                            }
                        }
                    }
                }"));

            return OfferCollection.Aggregate(pipeline).ToListAsync();
        }

        [Obsolete]
        public Task<List<GroupedOffersByPos>> GetOffersWithCover() {
            var pipeline = new EmptyPipelineDefinition<Offer>()
                .Match(Builders<Offer>.Filter.And(
                    Builders<Offer>.Filter.Ne(o => o.Pos.CoverPath, null),
                    Builders<Offer>.Filter.Ne(o => o.Pos.CoverBlurHash, null)
                ))
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
        /// Retrieve all offers of a POS.
        /// </summary>
        public Task<List<Offer>> GetOffersOfPos(ObjectId posId) {
            return OfferCollection.Find(Builders<Offer>.Filter.Eq(o => o.Pos.Id, posId)).ToListAsync();
        }

        /// <summary>
        /// Retrieve all offers of a POS with an optional last usage date.
        /// </summary>
        public async Task<(List<Offer>, Dictionary<ObjectId, DateTime> lastUsage)> GetOffersOfPosForAdmin(ObjectId posId) {
            var pipeline = new BsonDocument[] {
                new("$match",
                    new BsonDocument("pos._id", new BsonDocument("$eq", posId))
                ),
                new("$lookup",
                    new BsonDocument {
                        { "from", "PaymentRequests" },
                        { "localField", "payment.otc" },
                        { "foreignField", "_id" },
                        { "as", "paymentRequests" }
                    }
                ),
                new ("$addFields",
                    // Extract first element of array as separate field
                    new BsonDocument("paymentRequest",
                        new BsonDocument("$arrayElemAt",
                            new BsonArray { "$paymentRequests", 0 }
                        )
                    )
                ),
                new("$project",
                    new BsonDocument {
                        // Include basic structure of the Offer class
                        { "_id", 1 },
                        { "title", 1 },
                        { "description", 1 },
                        { "payment", 1 },
                        { "pos", 1 },
                        { "merchant", 1 },
                        { "createdOn", 1 },
                        { "lastUpdate", 1 },
                        { "deactivated", 1 },

                        // Extended information
                        { "latestPaymentConfirmation",
                            new BsonDocument("$arrayElemAt",
                                new BsonArray {
                                    "$paymentRequest.confirmations.performedAt",
                                    -1
                                }
                            )
                        },
                    }
                )
            };

            var bsonResults = await OfferCollection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            List<Offer> offers = [];
            Dictionary<ObjectId, DateTime> lastUsages = [];
            foreach(BsonDocument bsonDocument in bsonResults) {
                var offer = BsonSerializer.Deserialize<Offer>(bsonDocument);
                offers.Add(offer);

                if(bsonDocument.Contains("latestPaymentConfirmation") && bsonDocument["latestPaymentConfirmation"].IsBsonDateTime) {
                    lastUsages.Add(offer.Id, bsonDocument["latestPaymentConfirmation"].AsBsonDateTime.ToUniversalTime());
                }
            }

            return (offers, lastUsages);
        }

        /// <summary>
        /// Get an offer by its ID.
        /// </summary>
        public Task<Offer> GetOfferById(ObjectId offerId) {
            return OfferCollection.Find(Builders<Offer>.Filter.Eq(o => o.Id, offerId)).SingleOrDefaultAsync();
        }

        public Task<long> CountActiveOffersOfPos(ObjectId posId) {
            return OfferCollection.CountDocumentsAsync(Builders<Offer>.Filter.And(
                Builders<Offer>.Filter.Eq(o => o.Pos.Id, posId),
                Builders<Offer>.Filter.Ne(o => o.Deactivated, true)
            ));
        }

        /// <summary>
        /// Delete an offer by its ID.
        /// </summary>
        public Task DeleteOffer(ObjectId offerId) {
            return OfferCollection.DeleteOneAsync(Builders<Offer>.Filter.Eq(o => o.Id, offerId));
        }

        /// <summary>
        /// Delete all offers of a POS.
        /// </summary>
        public async Task<long> DeleteOffersByPos(ObjectId posId) {
            var result = await OfferCollection.DeleteManyAsync(Builders<Offer>.Filter.Eq(o => o.Pos.Id, posId));
            return result.DeletedCount;
        }

        /// <summary>
        /// Bulk updates POS information on all of its offers.
        /// </summary>
        public Task UpdatePosInformation(ObjectId posId, string name, string description, double? latitude, double? longitude, string url, bool posActive) {
            var position = (latitude.HasValue && longitude.HasValue) ?
                new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(longitude.Value, latitude.Value)) :
                null;

            return OfferCollection.UpdateManyAsync(
                Builders<Offer>.Filter.Eq(o => o.Pos.Id, posId),
                Builders<Offer>.Update
                    .Set(o => o.Pos.Name, name)
                    .Set(o => o.Pos.Description, description)
                    .Set(o => o.Pos.Position, position)
                    .Set(o => o.Pos.Url, url)
                    .Set(o => o.Deactivated, !posActive)
            );
        }

        /// <summary>
        /// Bulk updates cover path and blur hash on all offers by a given POS.
        /// </summary>
        public Task UpdatePosCovers(ObjectId posId, string coverPath, string coverBlurHash) {
            return OfferCollection.UpdateManyAsync(
                Builders<Offer>.Filter.Eq(o => o.Pos.Id, posId),
                Builders<Offer>.Update.Set(o => o.Pos.CoverPath, coverPath).Set(o => o.Pos.CoverBlurHash, coverBlurHash)
            );
        }

        public async Task MigratePaymentInformationInOffers() {
            List<WriteModel<Offer>> writes = new();

            var offers = await OfferCollection.Find(Builders<Offer>.Filter.Empty).ToListAsync();
            foreach(var o in offers) {
                var payment = await PaymentRequestCollection.Find(
                    Builders<PaymentRequest>.Filter.Eq(pr => pr.Otc, o.PaymentRequestId)
                ).SingleOrDefaultAsync();
                if(payment == null) {
                    Logger.LogError("Offer payment not found");
                    throw new Exception();
                }

                o.Payment = new Offer.PaymentInformation {
                    Otc = payment.Otc,
                    Password = payment.Password,
                    Cost = o.Cost,
                    Filter = o.Filter,
                };
                Logger.LogInformation("Upgrading offer {0}", o.Id);

                writes.Add(
                    new ReplaceOneModel<Offer>(Builders<Offer>.Filter.Eq(o => o.Id, o.Id), o)
                );
            }

            Logger.LogDebug("Performing bulk updates");
            await OfferCollection.BulkWriteAsync(writes);
        }

        /// <summary>
        ///     Get list of active offers
        /// </summary>
        public async Task<List<Offer>> GetActiveOffers() {
            try {
                List<BsonDocument> pipeline = new List<BsonDocument>();

                pipeline.Add(
                    new BsonDocument("$match",
                        new BsonDocument("$or",
                            new BsonArray {
                                new BsonDocument("deactivated", false),
                                new BsonDocument("deactivated",
                                    new BsonDocument("$exists", false))
                            }))
                );


                IAsyncCursor<BsonDocument> result = await OfferCollection.AggregateAsync<BsonDocument>(pipeline);
                List<BsonDocument> listOfActiveOffers = await result.ToListAsync();

                // Map to a strongly-typed model
                List<Offer> merchantOffers = listOfActiveOffers.Select(doc => new Offer {
                    Id = doc["_id"].AsObjectId,
                    Title = doc.Contains("title") && !doc["title"].IsBsonNull ? doc["title"].AsString : string.Empty,
                    Description = doc.Contains("description") && !doc["description"].IsBsonNull ? doc["description"].AsString : string.Empty,

                    Payment = doc.Contains("payment") && doc["payment"].IsBsonDocument
                        ? new Offer.PaymentInformation {
                            Otc = doc["payment"].AsBsonDocument.Contains("otc") && !doc["payment"]["otc"].IsBsonNull ? doc["payment"]["otc"].AsGuid : Guid.Empty,
                            Password = doc["payment"].AsBsonDocument.Contains("password") && !doc["payment"]["password"].IsBsonNull ? doc["payment"]["password"].AsString : string.Empty,
                            Cost = doc["payment"].AsBsonDocument.Contains("cost") && !doc["payment"]["cost"].IsBsonNull ? doc["payment"]["cost"].ToInt32() : 0
                        }
                        : null,

                    Merchant = doc.Contains("merchant") && doc["merchant"].IsBsonDocument
                        ? new Offer.MerchantInformation {
                            Id = doc["merchant"].AsBsonDocument.Contains("id") && !doc["merchant"]["id"].IsBsonNull ? doc["merchant"]["id"].AsObjectId : ObjectId.Empty,
                            Name = doc["merchant"].AsBsonDocument.Contains("name") && !doc["merchant"]["name"].IsBsonNull ? doc["merchant"]["name"].AsString : string.Empty,
                            WebsiteUrl = doc["merchant"].AsBsonDocument.Contains("website") && !doc["merchant"]["website"].IsBsonNull ? doc["merchant"]["website"].AsString : string.Empty
                        }
                        : null

                }).ToList();

                return merchantOffers;
            }
            catch(Exception ex) {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MerchantOfferDTO>> GetOffersRank(
            DateTime? startDate,
            DateTime? endDate,
            ObjectId[] merchantId) {
            try {
                var pipeline = new List<BsonDocument> {
                    // $match – filtra le offerte disattivate
                    new BsonDocument("$match",
                        new BsonDocument("deactivated",
                            new BsonDocument("$ne", true))
                    ),


                    //  Carica la PaymentRequest relativa.
                    new BsonDocument("$lookup",
                        new BsonDocument {
                            { "from", "PaymentRequests" },
                            { "localField", "payment.otc" },
                            { "foreignField", "_id" },
                            { "as", "paymentRequests" }
                        }
                    ),

                    // Trasforma array $paymentRequests in campo singolo.
                    new BsonDocument("$addFields",
                        new BsonDocument("paymentRequest",
                            new BsonDocument("$arrayElemAt",
                                new BsonArray { "$paymentRequests", 0 }
                            )
                        )
                    ),

                    // Calcolo delle statistiche e proiezione dell'output.
                    new BsonDocument("$project",
                        new BsonDocument {
                            { "_id", 1 },
                            { "title", 1 },
                            { "deactivated", 1 },
                            { "createdOn", 1 },
                            { "lastUpdate", 1 },
                            { "description", 1 },
                            { "payment", 1 },
                            { "pos", 1 },
                            { "merchant", 1 }, {
                                "totalAmount",
                                new BsonDocument("$sum",
                                    new BsonDocument("$multiply",
                                        new BsonArray {
                                            "$paymentRequest.amount",
                                            new BsonDocument("$cond",
                                                new BsonDocument {
                                                    {
                                                        "if",
                                                        new BsonDocument("$isArray", "$paymentRequest.confirmations")
                                                    }, {
                                                        "then",
                                                        new BsonDocument("$size", "$paymentRequest.confirmations")
                                                    },
                                                    { "else", 0 }
                                                }
                                            )
                                        }
                                    )
                                )
                            }, {
                                "transactionCount",
                                new BsonDocument("$cond",
                                    new BsonDocument {
                                        {
                                            "if",
                                            new BsonDocument("$isArray", "$paymentRequest.confirmations")
                                        }, {
                                            "then",
                                            new BsonDocument("$size", "$paymentRequest.confirmations")
                                        },
                                        { "else", 0 }
                                    }
                                )
                            }
                        }
                    ),

                    // ordina per totalAmount discendente
                    new BsonDocument("$sort",
                        new BsonDocument("totalAmount", -1)
                    )
                };


                IAsyncCursor<BsonDocument> result = await OfferCollection.AggregateAsync<BsonDocument>(pipeline);
                List<BsonDocument> offersRank = await result.ToListAsync();

                List<MerchantOfferDTO> merchantOffersRank = offersRank.Select(doc => new MerchantOfferDTO {
                    Id = doc["_id"].AsObjectId,
                    Title = doc.Contains("title") && !doc["title"].IsBsonNull ? doc["title"].AsString : string.Empty,
                    Description = doc.Contains("description") && !doc["description"].IsBsonNull ? doc["description"].AsString : string.Empty,
                    Pos = doc.Contains("pos") && !doc["pos"].IsBsonNull ? BsonSerializer.Deserialize<Pos>(doc["pos"].AsBsonDocument) : null,
                    Merchant = doc.Contains("merchant") && !doc["merchant"].IsBsonNull ? BsonSerializer.Deserialize<Merchant>(doc["merchant"].AsBsonDocument) : null,
                    TotalAmount = doc.Contains("totalAmount") && !doc["totalAmount"].IsBsonNull ? doc["totalAmount"].ToInt32() : 0,
                    TransactionsNumber = doc.Contains("transactionCount") && !doc["transactionCount"].IsBsonNull ? doc["transactionCount"].ToInt32() : 0,


                }).ToList();

                return merchantOffersRank;
            }
            catch(Exception ex) {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }
    }

}
