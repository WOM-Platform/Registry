﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {

    public class MerchantService {

        private readonly MongoClient _client;
        private readonly ILogger<MerchantService> _logger;

        public MerchantService(
            MongoClient client,
            ILogger<MerchantService> logger
        ) {
            _client = client;
            _logger = logger;
        }

        private IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
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
            var options = new FindOptions {
                Collation = new Collation("en", strength: CollationStrength.Secondary, caseLevel: false)
            };
            return MerchantCollection.Find(filter, options).SingleOrDefaultAsync();
        }

        /// <summary>
        /// Gets a list of merchants that the user controls as an administrator.
        /// </summary>
        public Task<List<Merchant>> GetMerchantsWithAdminControl(ObjectId userId) {
            var merchFilter = Builders<Merchant>.Filter.AnyEq(m => m.AdministratorIds, userId);
            return MerchantCollection.Find(merchFilter).ToListAsync();
        }

        /// <summary>
        /// Replace an existing merchant, by ID.
        /// </summary>
        public Task ReplaceMerchant(Merchant merchant) {
            var filter = Builders<Merchant>.Filter.Eq(u => u.Id, merchant.Id);
            return MerchantCollection.ReplaceOneAsync(filter, merchant);
        }

        public class MerchantWithAdmins : Merchant {
            [BsonElement("adminUsers")]
            public User[] Administrators { get; set; }
        }

        /// <summary>
        /// Fetches a list of all merchants with associated admin users.
        /// </summary>
        public Task<List<MerchantWithAdmins>> GetAllMerchantsWithUsers() {
            var pipeline = new EmptyPipelineDefinition<Merchant>()
                .AppendStage<Merchant, Merchant, MerchantWithAdmins>(BsonDocument.Parse(@"{
                    $lookup: {
                        from: 'Users',
                        localField: 'adminUserIds',
                        foreignField: '_id',
                        as: 'adminUsers'
                    }
                }"))
                .AppendStage<Merchant, MerchantWithAdmins, MerchantWithAdmins>(BsonDocument.Parse(@"{
                    $match: {
                        'adminUsers.0': {
                            $exists: true
                        }
                    }
                }"))
                .Sort(Builders<MerchantWithAdmins>.Sort.Ascending(m => m.Name))
            ;

            return MerchantCollection.Aggregate(pipeline).ToListAsync();
        }

    }

}