using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class OfferService : BaseService {

        public OfferService(
            MongoClient client,
            IConfiguration configuration,
            ILogger<BackupService> logger
        ) : base(client, logger) {
        }

        private IMongoCollection<Migration> OfferCollection {
            get {
                return MainDatabase.GetCollection<Migration>("Offers");
            }
        }

    }
}
