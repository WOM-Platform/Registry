using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class PaymentService : BaseService {

        public PaymentService(
            MongoClient client,
            IConfiguration configuration,
            ILogger<BackupService> logger
        ) : base(client, logger) {
        }

        private IMongoCollection<PaymentRequest> PaymentRequestCollection {
            get {
                return MainDatabase.GetCollection<PaymentRequest>("PaymentRequests");
            }
        }

        public Task<List<PaymentRequest>> GetPersistentPayments() {
            var filter = Builders<PaymentRequest>.Filter.And(
                Builders<PaymentRequest>.Filter.Eq(pr => pr.Verified, true),
                Builders<PaymentRequest>.Filter.Eq(pr => pr.IsPersistent, true)
            );

            return PaymentRequestCollection.Find(filter).ToListAsync();
        }

    }
}
