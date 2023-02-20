using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {

    public class MongoDatabase {

        private readonly MongoClient _client;
        private readonly ILogger<MongoDatabase> _logger;

        public MongoDatabase(
            MongoClient client,
            ILogger<MongoDatabase> logger
        ) {
            _client = client;
            _logger = logger;
        }

        private IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
        }

        private IMongoCollection<GenerationRequest> GenerationCollection {
            get {
                return MainDatabase.GetCollection<GenerationRequest>("GenerationRequests");
            }
        }

        private IMongoCollection<PaymentRequest> PaymentCollection {
            get {
                return MainDatabase.GetCollection<PaymentRequest>("PaymentRequests");
            }
        }

        private IMongoCollection<Source> SourceCollection {
            get {
                return MainDatabase.GetCollection<Source>("Sources");
            }
        }

        private IMongoCollection<User> UserCollection {
            get {
                return MainDatabase.GetCollection<User>("Users");
            }
        }

        public Task<User> GetUserById(ObjectId id) {
            var filter = Builders<User>.Filter.Eq(u => u.Id, id);
            return UserCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task<User> GetUserByEmail(string email) {
            var filter = Builders<User>.Filter.Eq(u => u.Email, email);
            var options = new FindOptions {
                Collation = new Collation("en", strength: CollationStrength.Secondary, caseLevel: false)
            };
            return UserCollection.Find(filter, options).SingleOrDefaultAsync();
        }

        public Task CreateUser(User user) {
            return UserCollection.InsertOneAsync(user);
        }

        public Task ReplaceUser(User user) {
            var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
            return UserCollection.ReplaceOneAsync(filter, user);
        }

        public Task UpdateUser(ObjectId userId,
            string name = null,
            string surname = null,
            string email = null
        ) {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);

            var chain = Builders<User>.Update.Chain();
            if(name != null) chain.Set(u => u.Name, name);
            if(surname != null) chain.Set(u => u.Surname, surname);
            if(email != null) chain.Set(u => u.Email, email);

            return UserCollection.UpdateOneAsync(filter, chain.End());
        }

        private IMongoCollection<Voucher> VoucherCollection {
            get {
                return MainDatabase.GetCollection<Voucher>("Vouchers");
            }
        }

        public Task AddVouchers(IEnumerable<Voucher> vouchers) {
            return VoucherCollection.InsertManyAsync(vouchers);
        }

        public Task ReplaceVouchers(IEnumerable<Voucher> vouchers) {
            var replaces = from v in vouchers
                           let filter = Builders<Voucher>.Filter.Eq(vf => vf.Id, v.Id)
                           select new ReplaceOneModel<Voucher>(filter, v);
            return VoucherCollection.BulkWriteAsync(replaces);
        }

        public Task UpdateVoucherLocation(IEnumerable<Voucher> vouchers, GeoJsonPoint<GeoJson2DGeographicCoordinates> location) {
            var voucherIds = (from v in vouchers select v.Id).ToArray();
            var update = Builders<Voucher>.Update.Set(v => v.Position, location);
            return VoucherCollection.UpdateManyAsync<Voucher>(v => voucherIds.Contains(v.Id), update);
        }

        public Task<List<Voucher>> GetVouchersByGenerationRequest(Guid otcGen) {
            var filter = Builders<Voucher>.Filter.Eq(v => v.GenerationRequestId, otcGen);
            return VoucherCollection.Find(filter).ToListAsync();
        }

        public Task<List<Voucher>> GetVouchersWithIds(IEnumerable<ObjectId> ids) {
            var filter = Builders<Voucher>.Filter.In(v => v.Id, ids);
            return VoucherCollection.Find(filter).ToListAsync();
        }

        public class VoucherStats {
            public long TotalCount;
            public long AvailableCount;
            public Dictionary<string, long> IssuedByAim;
        }

        private class VoucherStatsDoc {
            [BsonId]
            public string AimCode;
            [BsonElement("totalCount")]
            public long TotalCount;
            [BsonElement("availableCount")]
            public long AvailableCount;
        }

        public async Task<VoucherStats> GetVoucherStats() {
            PipelineDefinition<Voucher, VoucherStatsDoc> pipeline = new BsonDocument[] {
                BsonDocument.Parse(@"{ $group: {
                    _id: ""$aimCode"",
                    ""totalCount"": { $sum: ""$initialCount"" },
                    ""availableCount"": { $sum: ""$count"" }
                    }
                }")
            };

            var stats = await VoucherCollection.Aggregate(pipeline).ToListAsync();
            var ret = new VoucherStats {
                IssuedByAim = new Dictionary<string, long>()
            };
            foreach(var s in stats) {
                ret.TotalCount += s.TotalCount;
                ret.AvailableCount += s.AvailableCount;
                ret.IssuedByAim[s.AimCode] = s.TotalCount;
            }

            return ret;
        }

        private IMongoCollection<LegacyVoucher> LegacyVoucherCollection {
            get {
                return MainDatabase.GetCollection<LegacyVoucher>("LegacyVouchers");
            }
        }

        public Task AddLegacyVouchers(IEnumerable<LegacyVoucher> vouchers) {
            return LegacyVoucherCollection.InsertManyAsync(vouchers);
        }

        public Task<List<LegacyVoucher>> GetLegacyVouchersWithIds(IEnumerable<long> ids) {
            var filter = Builders<LegacyVoucher>.Filter.In(v => v.Id, ids);
            return LegacyVoucherCollection.Find(filter).ToListAsync();
        }

        public Task ReplaceLegacyVouchers(IEnumerable<LegacyVoucher> vouchers) {
            var replaces = from v in vouchers
                           let filter = Builders<LegacyVoucher>.Filter.Eq(vf => vf.Id, v.Id)
                           select new ReplaceOneModel<LegacyVoucher>(filter, v);
            return LegacyVoucherCollection.BulkWriteAsync(replaces);
        }

    }

}
