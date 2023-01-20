using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class UserService {

        private readonly MongoClient _client;
        private readonly ILogger<PosService> _logger;

        public UserService(
            MongoClient client,
            ILogger<PosService> logger
        ) {
            _client = client;
            _logger = logger;
        }

        private IMongoDatabase MainDatabase {
            get {
                return _client.GetDatabase("Wom");
            }
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
            var options = new FindOptions {
                Collation = new Collation("en", strength: CollationStrength.Secondary, caseLevel: false)
            };
            return UserCollection.Find(filter, options).SingleOrDefaultAsync();
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

    }
}
