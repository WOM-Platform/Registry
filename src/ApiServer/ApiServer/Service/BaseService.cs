using System;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class BaseService {
        private readonly MongoClient _client;
        private readonly Random _random;
        private readonly ILogger<BaseService> _logger;

        protected BaseService(
            MongoClient client,
            ILogger<BaseService> logger
        ) {
            _client = client;
            _logger = logger;

            _random = new Random();
        }

        protected IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
        }

        protected IMongoCollection<ApiKey> ApiKeyCollection {
            get {
                return MainDatabase.GetCollection<ApiKey>("ApiKeys");
            }
        }

        protected IMongoCollection<GenerationRequest> GenerationRequestCollection {
            get {
                return MainDatabase.GetCollection<GenerationRequest>("GenerationRequests");
            }
        }

        protected IMongoCollection<LegacyVoucher> LegacyVoucherCollection {
            get {
                return MainDatabase.GetCollection<LegacyVoucher>("LegacyVouchers");
            }
        }

        protected IMongoCollection<Merchant> MerchantCollection {
            get {
                return MainDatabase.GetCollection<Merchant>("Merchants");
            }
        }

        protected IMongoCollection<Migration> MigrationCollection {
            get {
                return MainDatabase.GetCollection<Migration>("Migrations");
            }
        }

        protected IMongoCollection<PaymentRequest> PaymentRequestCollection {
            get {
                return MainDatabase.GetCollection<PaymentRequest>("PaymentRequests");
            }
        }

        protected IMongoCollection<Voucher> VoucherCollection {
            get {
                return MainDatabase.GetCollection<Voucher>("Vouchers");
            }
        }

        protected IMongoCollection<User> UserCollection {
            get {
                return MainDatabase.GetCollection<User>("Users");
            }
        }

        protected ILogger<BaseService> Logger { get { return _logger; } }

        protected Random Random { get { return _random; } }
    }
}
