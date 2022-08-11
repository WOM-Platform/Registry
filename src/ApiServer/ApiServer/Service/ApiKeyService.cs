using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Driver;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class ApiKeyService {

        private readonly MongoClient _client;
        private readonly ILogger<ApiKeyService> _logger;
        private readonly Random _rnd = new Random();

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

        public Task<ApiKey> RetrieveApiKey(string key) {
            var filter = Builders<ApiKey>.Filter.Eq(ak => ak.Key, key);
            var options = new FindOptions {
                Collation = new Collation("simple")
            };
            return ApiKeyCollection.Find(filter, options).SingleOrDefaultAsync();
        }

        public async Task<ApiKey> CreateOrGetApiKey(ObjectId controlledEntityId, string keySelector, ApiKey.KindOfKey kind) {
            var filter = Builders<ApiKey>.Filter.And(
                Builders<ApiKey>.Filter.Eq(ak => ak.ControlledEntityId, controlledEntityId),
                Builders<ApiKey>.Filter.Eq(ak => ak.KeySelector, keySelector)
            );
            var options = new FindOptions {
                Collation = new Collation("simple")
            };
            var matchingKey = await ApiKeyCollection.Find(filter, options).SingleOrDefaultAsync();

            if(matchingKey == null) {
                var keys = CryptoHelper.CreateKeyPair();

                matchingKey = new ApiKey {
                    Key = _rnd.GenerateCode(16),
                    ControlledEntityId = controlledEntityId,
                    KeySelector = keySelector,
                    Kind = kind,
                    Expired = false,
                    PublicKey = keys.Public.ToPemString(),
                    PrivateKey = keys.Private.ToPemString(),
                };

                await ApiKeyCollection.InsertOneAsync(matchingKey);
            }
            else if(matchingKey.Kind != kind || matchingKey.Expired) {
                matchingKey.Kind = kind;
                matchingKey.Expired = false;

                await ApiKeyCollection.ReplaceOneAsync(Builders<ApiKey>.Filter.Eq(ak => ak.Id, matchingKey.Id), matchingKey);
            }

            return matchingKey;
        }

        /// <summary>
        /// Gets the public key of an entity through an API key, performing safety checks.
        /// If no API key is passed, a default key is used instead.
        /// </summary>
        /// <param name="apiKey">API key to lookup.</param>
        /// <param name="entityId">Identity of the entity that the API key must control to obtain the key.</param>
        /// <param name="defaultKey">Default public key to use if no API key is passed.</param>
        public async Task<AsymmetricKeyParameter> GetPublicKey(string apiKey, ObjectId entityId, string defaultKey) {
            if(string.IsNullOrEmpty(apiKey)) {
                return CryptoHelper.LoadKeyFromString<AsymmetricKeyParameter>(defaultKey);
            }
            else {
                var entry = await RetrieveApiKey(apiKey);
                if(entry == null) {
                    _logger.LogInformation("API key {0} not registered", apiKey);
                    return null;
                }

                if(entry.ControlledEntityId != entityId) {
                    _logger.LogWarning("API key {0} does not control entity {1}", apiKey, entityId);
                    return null;
                }

                return CryptoHelper.LoadKeyFromString<AsymmetricKeyParameter>(entry.PublicKey);
            }
        }
    }
}
