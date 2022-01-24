using System.Collections.Generic;
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

        public class VoucherCountByAimResult {
            [BsonId]
            public string AimCode;
            [BsonElement("totalCount")]
            public long TotalCount;
            [BsonElement("availableCount")]
            public long AvailableCount;
        };

        public async Task<List<VoucherCountByAimResult>> GetVoucherCountByAim() {
            PipelineDefinition<Voucher, VoucherCountByAimResult> pipeline = new BsonDocument[] {
                BsonDocument.Parse(@"{
                    $match: {
                        ""aimCode"": { $ne: """" }
                    }
                }"),
                BsonDocument.Parse(@"{
                    $group: {
                        _id: ""$aimCode"",
                        ""totalCount"": { $sum: ""$initialCount"" },
                        ""availableCount"": { $sum: { $ifNull:[ ""$count"", ""$initialCount"" ] } }
                    }
                }")
            };

            return await VoucherCollection.Aggregate(pipeline).ToListAsync();
        }

    }

}
