using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class ApiKeyService {

        private readonly MongoClient _client;
        private readonly ILogger<ApiKeyService> _logger;

        public ApiKeyService(
            MongoClient client,
            ILogger<ApiKeyService> logger
        ) {
            _client = client;
            _logger = logger;
        }

        private IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
        }

        private IMongoCollection<ApiKey> ApiKeyCollection {
            get {
                return MainDatabase.GetCollection<ApiKey>("ApiKeys");
            }
        }

        public Task RegisterApiKey(ApiKey apiKey) {
            return ApiKeyCollection.InsertOneAsync(apiKey);
        }

        public Task<ApiKey> RetrieveApiKey(string key) {
            var filter = Builders<ApiKey>.Filter.Eq(ak => ak.Key, key);
            var options = new FindOptions {
                Collation = new Collation("simple")
            };
            return ApiKeyCollection.Find(filter, options).SingleOrDefaultAsync();
        }

    }
}
