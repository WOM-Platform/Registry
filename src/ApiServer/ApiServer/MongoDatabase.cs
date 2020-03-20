﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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

                            _logger.LogInformation("Creating new Mongo client");
                            _client = new MongoClient(string.Format("mongodb://{0}:{1}@mongo", username, password));
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

        public Task<List<Aim>> GetAims() {
            var filter = Builders<Aim>.Filter.Ne(a => a.Hidden, true);
            return AimCollection.Find(filter)
                .SortBy(a => a.Order)
                .ThenBy(a => a.Code)
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

        public Task<List<Merchant>> GetMerchantsByUser(ObjectId userId) {
            var merchFilter = Builders<Merchant>.Filter.AnyEq(m => m.AdministratorUserIds, userId);
            return MerchantCollection.Find(merchFilter).ToListAsync();
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

        public async Task<List<Pos>> GetPosByUser(ObjectId userId) {
            var merchantIds = from m in await GetMerchantsByUser(userId)
                              select m.Id;

            var posFilter = Builders<Pos>.Filter.In(p => p.MerchantId, merchantIds);
            return await PosCollection.Find(posFilter).ToListAsync();
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

    }

}
