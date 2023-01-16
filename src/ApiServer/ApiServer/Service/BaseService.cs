using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace WomPlatform.Web.Api.Service {
    public class BaseService {
        private readonly MongoClient _client;
        private readonly ILogger<BaseService> _logger;

        protected BaseService(
            MongoClient client,
            ILogger<BaseService> logger
        ) {
            _client = client;
            _logger = logger;
        }

        protected IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
        }

        protected ILogger<BaseService> Logger { get { return _logger; } }
    }
}
