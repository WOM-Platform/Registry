﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {

    public class SourceService {

        private readonly MongoClient _client;
        private readonly ILogger<SourceService> _logger;

        public SourceService(
            MongoClient client,
            ILogger<SourceService> logger
        ) {
            _client = client;
            _logger = logger;
        }

        private IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
        }

        private IMongoCollection<Source> SourceCollection {
            get {
                return MainDatabase.GetCollection<Source>("Sources");
            }
        }

        private IMongoCollection<GenerationRequest> GenerationRequestCollection {
            get {
                return MainDatabase.GetCollection<GenerationRequest>("GenerationRequests");
            }
        }

        public Task<Source> GetSourceById(ObjectId id) {
            var filter = Builders<Source>.Filter.Eq(m => m.Id, id);
            return SourceCollection.Find(filter).SingleOrDefaultAsync();
        }

        public class SourcetWithAdmins : Source {
            [BsonElement("adminUsers")]
            public User[] Administrators { get; set; }
        }

        public class GeneratedVouchersCountBySourceResult {
            [BsonId]
            public int? Id;
            [BsonElement("total")]
            public long Total;
        }

        /// <summary>
        /// Returns the sum of vouchers that have been generated by the selected Source.
        /// </summary>
        /// <param name="sourceId"></param>
        /// <returns></returns>
        public async Task<GeneratedVouchersCountBySourceResult> GetGeneratedVouchersBySource(ObjectId sourceId) {
            PipelineDefinition<GenerationRequest, GeneratedVouchersCountBySourceResult> pipeline = new BsonDocument[] {
                BsonDocument.Parse(@"{
                    $project: {
                        'GenerationRequests': '$$ROOT', '_id': 0
                    }
                }"),
                BsonDocument.Parse(@"{
                    $lookup: {
                        localField: 'GenerationRequests._id',
                        from: 'Vouchers',
                        foreignField: 'generationRequestId',
                        as: 'Vouchers'
                    }
                }"),
                BsonDocument.Parse(@"{
                    $unwind: {
                        path: '$Vouchers',
                        preserveNullAndEmptyArrays: true
                    }
                }"),
                BsonDocument.Parse(@"{
                    $addFields: {
                        'gr_id': '$GenerationRequests._id',
                        'gr_sourceId': '$GenerationRequests.sourceId',
                        'gr_amount': '$GenerationRequests.amount',
                        'vo_id': '$Vouchers.position',
                        'vo_initialCount': '$Vouchers.initialCount',
                        'vo_count': '$Vouchers.count',
                        'total_amount': {
                            $multiply: ['$Vouchers.initialCount', '$GenerationRequests.amount']
                        }
                    }
                }"),
                BsonDocument.Parse("{$match: {'GenerationRequests.sourceId': {$eq: new ObjectId('" + sourceId + "')}}}"),
                BsonDocument.Parse(@"{
                    $group: {
                        _id: null,
                        total: {$sum: '$total_amount'}
                    }
                }")
            };

            return await GenerationRequestCollection.Aggregate(pipeline).SingleOrDefaultAsync();
        }

        /*
           db.getSiblingDB("Wom").getCollection("GenerationRequests").aggregate([
           {
               $project: { "GenerationRequests": "$$ROOT", "_id": 0}
               },
           {
               $lookup:
                   {
                   localField: "GenerationRequests._id",
               from: "Vouchers",
               foreignField: "generationRequestId",
               as: "Vouchers"
               }
               },
           {
               $unwind:
                   {
                   path: "$Vouchers",
               preserveNullAndEmptyArrays: true
               }
               },
           {
               $addFields: { "gr_id": "$GenerationRequests._id", "gr_sourceId": "$GenerationRequests.sourceId", "gr_amount": "$GenerationRequests.amount", "vo_id": "$Vouchers.position", "vo_initialCount": "$Vouchers.initialCount", "vo_count": "$Vouchers.count", "total_amount": {$multiply: ["$Vouchers.initialCount", "$GenerationRequests.amount"]} }
               },
           {
               $match: { "GenerationRequests.sourceId": {$eq: new ObjectId("5e737c3a9036edb02d61ee34")} }
               },
           // {
           //  $project: {"gr_id": 1, "gr_sourceId": 1, "gr_amount": 1, "vo_id": 1, "vo_initialCount": 1, "vo_count": 1, "total_amount": 1, "total": 1, "_id": 0}
           // },
           {
           $group:
                   {
                   _id: null,
               total: {$sum: "$total_amount"}
                   }
               }
           ])*/

    }

}
