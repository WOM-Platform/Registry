﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {

    public class StatsService {

        private readonly MongoClient _client;
        private readonly ILogger<StatsService> _logger;

        public StatsService(
            MongoClient client,
            ILogger<StatsService> logger
        ) {
            _client = client;
            _logger = logger;
        }

        private IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
        }

        private IMongoCollection<Voucher> VoucherCollection {
            get {
                return MainDatabase.GetCollection<Voucher>("Vouchers");
            }
        }

        private IMongoCollection<GenerationRequest> GenerationRequestCollection {
            get {
                return MainDatabase.GetCollection<GenerationRequest>("GenerationRequests");
            }
        }

        public class VoucherCountByAimResult {
            [BsonId]
            public string AimCode;
            [BsonElement("totalCount")]
            public long TotalCount;
            [BsonElement("availableCount")]
            public long AvailableCount;
            [BsonElement("redeemedCount")]
            public long RedeemedCount;
        }

        public async Task<List<VoucherCountByAimResult>> GetVoucherCountByAim() {
            PipelineDefinition<Voucher, VoucherCountByAimResult> pipeline = new BsonDocument[] {
                BsonDocument.Parse(@"{
                    $match: {
                        'aimCode': { $ne: '' }
                    }
                }"),
                BsonDocument.Parse(@"{
                    $lookup: {
                        from: 'GenerationRequests',
                        localField: 'generationRequestId',
                        foreignField: '_id',
                        as: 'generationRequests'
                    }
                }"),
                BsonDocument.Parse(@"{
                    $set: {
                        'generationRequest': { $arrayElemAt: [ '$generationRequests', 0] }
                    }
                }"),
                BsonDocument.Parse(@"{
                    $group: {
                        _id: '$aimCode',
                        'totalCount': { $sum: '$initialCount' },
                        'availableCount': { $sum: { $ifNull:[ '$count', '$initialCount' ] } },
                        'redeemedCount': {
                            $sum: { 
                                $cond: {
                                    if: '$generationRequest.performedAt',
                                    then: '$initialCount',
                                    else: 0
                                }
                            }
                        }
                    }
                }"),
                BsonDocument.Parse(@"{
                    $sort: {
                        _id: 1
                    }
                }")
            };

            return await VoucherCollection.Aggregate(pipeline).ToListAsync();
        }

        public class VoucherCountBySourceResult {
            [BsonId]
            public int Id;
            [BsonElement("generationRequests")]
            public long GenerationRequests;
            [BsonElement("totalCount")]
            public long TotalCount;
            [BsonElement("redeemedCount")]
            public long RedeemedCount;
        }

        public async Task<VoucherCountBySourceResult> GetVoucherCountBySource(ObjectId sourceId) {
            PipelineDefinition<GenerationRequest, VoucherCountBySourceResult> pipeline = new BsonDocument[] {
                BsonDocument.Parse(string.Format(@"{{
                    $match: {{
                        'sourceId': ObjectId('{0}')
                    }}
                }}", sourceId.ToString())),
                BsonDocument.Parse(@"{
                    $group: {
                        _id: 1,
                        'generationRequests': {
                            $sum: 1
                        },
                        'totalCount': {
                            $sum: '$totalVoucherCount'
                        },
                        'redeemedCount': {
                            $sum: {
                                $cond: {
                                    if: '$performedAt',
                                    then: '$totalVoucherCount',
                                    else: 0
                                }
                            }
                        }
                    }
                }")
            };

            return await GenerationRequestCollection.Aggregate(pipeline).SingleOrDefaultAsync();
        }

    }

}
