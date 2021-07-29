using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api {

    public class MongoDatabase {

        private readonly ILogger<MongoDatabase> _logger;

        public MongoDatabase(
            ILogger<MongoDatabase> logger) {
            _logger = logger;
        }

        private readonly object _lockRoot = new object();
        private MongoClient _client = null;

        private MongoClient Client {
            get {
                if(_client == null) {
                    lock(_lockRoot) {
                        if(_client == null) {
                            var username = Environment.GetEnvironmentVariable("MONGO_INITDB_ROOT_USERNAME");
                            var password = Environment.GetEnvironmentVariable("MONGO_INITDB_ROOT_PASSWORD");
                            var host = Environment.GetEnvironmentVariable("MONGO_CONNECTION_HOST");
                            var port = Environment.GetEnvironmentVariable("MONGO_CONNECTION_PORT");

                            _logger.LogInformation("Creating new Mongo client");
                            _client = new MongoClient(string.Format("mongodb://{0}:{1}@{2}:{3}", username, password, host, port));
                        }
                    }
                }

                return _client;
            }
        }

        private IMongoDatabase MainDatabase {
            get {
                return Client.GetDatabase("Wom");
            }
        }

        private IMongoCollection<Aim> AimCollection {
            get {
                return MainDatabase.GetCollection<Aim>("Aims");
            }
        }

        /// <summary>
        /// Get all non-hidden aims.
        /// </summary>
        public Task<List<Aim>> GetAims() {
            return AimCollection.Aggregate()
                // Projects and adds length and root (first code character)
                .Project<BsonDocument>((ProjectionDefinition<Aim>)@"{
                    ""titles"": 1,
                    ""order"": 1,
                    ""hidden"": 1,
                    ""root"": { $substrCP:[ ""$_id"", 0, 1 ] }
                }")
                // Hide hidden aims
                .Match(@"{
                    ""hidden"": {$ne: true }
                }")
                // Sort by root aim
                .Sort(@"{
                    ""root"": 1,
                    ""_id"": 1
                }")
                .As<Aim>()
                .ToListAsync();
        }

        /// <summary>
        /// Get all non-hidden root aims.
        /// </summary>
        public Task<List<Aim>> GetRootAims() {
            return AimCollection.Aggregate()
                // Projects and adds length and root (first code character)
                .Project<BsonDocument>((ProjectionDefinition<Aim>)@"{
                    ""titles"": 1,
                    ""order"": 1,
                    ""hidden"": 1,
                    ""length"": {$strLenCP: ""$_id""},
                    ""root"": { $substrCP:[ ""$_id"", 0, 1 ] }
                }")
                // Match only aims with length == 1
                .Match(@"{
                    ""hidden"": {$ne: true },
                    ""length"": 1
                }")
                // Sort by root aim
                .Sort(@"{
                    ""root"": 1,
                    ""_id"": 1
                }")
                .As<Aim>()
                .ToListAsync();
        }

        public Task<Aim> GetAimByCode(string code) {
            var filter = Builders<Aim>.Filter.Eq(a => a.Code, code);
            return AimCollection.Find(filter).SingleOrDefaultAsync();
        }

        private IMongoCollection<GenerationRequest> GenerationCollection {
            get {
                return MainDatabase.GetCollection<GenerationRequest>("GenerationRequests");
            }
        }

        public Task AddGenerationRequest(GenerationRequest request) {
            return GenerationCollection.InsertOneAsync(request);
        }

        public Task<GenerationRequest> GetGenerationRequestByOtc(Guid otc) {
            var filter = Builders<GenerationRequest>.Filter.Eq(r => r.Otc, otc);
            return GenerationCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task UpdateGenerationRequest(GenerationRequest request) {
            var filter = Builders<GenerationRequest>.Filter.Eq(r => r.Otc, request.Otc);
            return GenerationCollection.ReplaceOneAsync(filter, request);
        }

        private IMongoCollection<Merchant> MerchantCollection {
            get {
                return MainDatabase.GetCollection<Merchant>("Merchants");
            }
        }

        public Task CreateMerchant(Merchant merchant) {
            return MerchantCollection.InsertOneAsync(merchant);
        }

        public Task<Merchant> GetMerchantById(ObjectId id) {
            var filter = Builders<Merchant>.Filter.Eq(m => m.Id, id);
            return MerchantCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task<Merchant> GetMerchantByFiscalCode(string fiscalCode) {
            var filter = Builders<Merchant>.Filter.Eq(m => m.FiscalCode, fiscalCode);
            return MerchantCollection.Find(filter).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Gets a list of merchants that the user controls as an administrator.
        /// </summary>
        public Task<List<Merchant>> GetMerchantsWithAdminControl(ObjectId userId) {
            var merchFilter = Builders<Merchant>.Filter.AnyEq(m => m.AdministratorIds, userId);
            return MerchantCollection.Find(merchFilter).ToListAsync();
        }

        /// <summary>
        /// Gets a list of merchants that the user controls as a POS user.
        /// </summary>
        public Task<List<Merchant>> GetMerchantsWithPosControl(ObjectId userId) {
            var merchFilter = Builders<Merchant>.Filter.Or(
                Builders<Merchant>.Filter.AnyEq(m => m.AdministratorIds, userId),
                Builders<Merchant>.Filter.AnyEq(m => m.PosUserIds, userId)
            );
            return MerchantCollection.Find(merchFilter).ToListAsync();
        }

        /// <summary>
        /// Replace an existing merchant, by ID.
        /// </summary>
        public Task ReplaceMerchant(Merchant merchant) {
            var filter = Builders<Merchant>.Filter.Eq(u => u.Id, merchant.Id);
            return MerchantCollection.ReplaceOneAsync(filter, merchant);
        }

        private IMongoCollection<PaymentRequest> PaymentCollection {
            get {
                return MainDatabase.GetCollection<PaymentRequest>("PaymentRequests");
            }
        }

        public Task<PaymentRequest> GetPaymentRequestByOtc(Guid otcPay) {
            var filter = Builders<PaymentRequest>.Filter.Eq(p => p.Otc, otcPay);
            return PaymentCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task UpdatePaymentRequest(PaymentRequest request) {
            var filter = Builders<PaymentRequest>.Filter.Eq(r => r.Otc, request.Otc);
            return PaymentCollection.ReplaceOneAsync(filter, request);
        }

        public Task AddPaymentRequest(PaymentRequest paymentRequest) {
            return PaymentCollection.InsertOneAsync(paymentRequest);
        }

        private IMongoCollection<Pos> PosCollection {
            get {
                return MainDatabase.GetCollection<Pos>("Pos");
            }
        }

        public Task CreatePos(Pos pos) {
            return PosCollection.InsertOneAsync(pos);
        }

        public Task<Pos> GetPosById(ObjectId id) {
            var filter = Builders<Pos>.Filter.Eq(u => u.Id, id);
            return PosCollection.Find(filter).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Gets list of POS that the user controls.
        /// </summary>
        public async Task<List<Pos>> GetPosByUser(ObjectId userId) {
            var merchantIds = from m in await GetMerchantsWithPosControl(userId)
                              select m.Id;

            var posFilter = Builders<Pos>.Filter.In(p => p.MerchantId, merchantIds);
            return await PosCollection.Find(posFilter).ToListAsync();
        }

        /// <summary>
        /// Get list of POS with position.
        /// </summary>
        public Task<List<Pos>> GetPosWithPosition() {
            var filter = Builders<Pos>.Filter.And(
                Builders<Pos>.Filter.Ne(pos => pos.IsDummy, true),
                Builders<Pos>.Filter.Exists(pos => pos.Position, true),
                Builders<Pos>.Filter.Ne(pos => pos.Position, null)
            );
            return PosCollection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// Gets list of Merchants and POS that the user controls.
        /// </summary>
        public async Task<List<(Merchant, List<Pos>)>> GetMerchantsAndPosByUser(ObjectId userId) {
            // Get all merchants with control
            var merchants = await GetMerchantsWithPosControl(userId);

            // Get all matching POS
            var posFilter = Builders<Pos>.Filter.In(p => p.MerchantId, from m in merchants select m.Id);
            var pos = await PosCollection.Find(posFilter).ToListAsync();

            // Build nested list
            var ret = new List<(Merchant, List<Pos>)>(merchants.Count);
            foreach(var merchant in merchants) {
                ret.Add((merchant, pos.Where(p => p.MerchantId == merchant.Id).ToList()));
            }

            return ret;
        }

        public Task<List<Pos>> GetPosByMerchant(ObjectId merchantId) {
            var posFilter = Builders<Pos>.Filter.Eq(p => p.MerchantId, merchantId);
            return PosCollection.Find(posFilter).ToListAsync();
        }

        /// <summary>
        /// Upserts a POS synchronously.
        /// </summary>
        public void UpsertPosSync(Pos pos) {
            var filter = Builders<Pos>.Filter.Eq(p => p.Id, pos.Id);
            PosCollection.ReplaceOne(filter, pos, new ReplaceOptions {
                IsUpsert = true
            });
        }

        /// <summary>
        /// Replace an existing POS, by ID.
        /// </summary>
        public Task ReplacePos(Pos pos) {
            var filter = Builders<Pos>.Filter.Eq(p => p.Id, pos.Id);
            return PosCollection.ReplaceOneAsync(filter, pos);
        }

        private IMongoCollection<Source> SourceCollection {
            get {
                return MainDatabase.GetCollection<Source>("Sources");
            }
        }

        public Task<Source> GetSourceById(ObjectId id) {
            var filter = Builders<Source>.Filter.Eq(u => u.Id, id);
            return SourceCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task<List<Source>> GetSourcesByUser(ObjectId userId) {
            var filter = Builders<Source>.Filter.AnyEq(s => s.AdministratorUserIds, userId);
            return SourceCollection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// Upserts a source synchronously.
        /// </summary>
        public void UpsertSourceSync(Source source) {
            var filter = Builders<Source>.Filter.Eq(s => s.Id, source.Id);
            SourceCollection.ReplaceOne(filter, source, new ReplaceOptions {
                IsUpsert = true
            });
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
            return UserCollection.Find(filter).SingleOrDefaultAsync();
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
