using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {

    /// <summary>
    /// Auxiliary service for setup procedures.
    /// </summary>
    public class SetupService {

        private readonly MongoClient _client;
        private readonly ILogger<SetupService> _logger;

        public SetupService(
            MongoClient client,
            ILogger<SetupService> logger
        ) {
            _client = client;
            _logger = logger;
        }

        private IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
        }

        private IMongoCollection<Source> SourceCollection {
            get {
                return MainDatabase.GetCollection<Source>("Sources");
            }
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

        private IMongoCollection<Pos> PosCollection {
            get {
                return MainDatabase.GetCollection<Pos>("Pos");
            }
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

        private IMongoCollection<User> UserCollection {
            get {
                return MainDatabase.GetCollection<User>("Users");
            }
        }

        /// <summary>
        /// Upserts a user synchronously.
        /// </summary>
        public void UpsertUserSync(User user) {
            var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
            UserCollection.ReplaceOne(filter, user, new ReplaceOptions {
                IsUpsert = true
            });
        }

    }

}
