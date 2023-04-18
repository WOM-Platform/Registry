using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Service {
    public class UserService : BaseService {

        private readonly MongoClient _client;
        private readonly ILogger<UserService> _logger;

        public UserService(
            MongoClient client,
            ILogger<UserService> logger
        ) : base(client, logger) {
            _client = client;
            _logger = logger;
        }

        public Task<User> GetUserById(ObjectId id) {
            var filter = Builders<User>.Filter.Eq(u => u.Id, id);
            return UserCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task<User> GetUserByEmail(string email) {
            var filter = Builders<User>.Filter.Eq(u => u.Email, email.Trim());
            var options = new FindOptions {
                Collation = new Collation("en", strength: CollationStrength.Secondary, caseLevel: false)
            };
            return UserCollection.Find(filter, options).SingleOrDefaultAsync();
        }

        public async Task<User> CreateUser(IClientSessionHandle session,
            string email, string name, string surname, string password,
            bool isVerified = false,
            PlatformRole platformRole = PlatformRole.User
        ) {
            var effectiveEmail = email.Trim();

            var existingUser = await GetUserByEmail(effectiveEmail);
            if(existingUser != null) {
                throw new ServiceProblemException("Email already registered", statusCode: StatusCodes.Status400BadRequest);
            }

            var user = new User {
                Email = effectiveEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Name = name.Trim(),
                Surname = surname.Trim(),
                VerificationToken = isVerified ? null : Random.GenerateReadableCode(8),
                Role = platformRole,
                RegisteredOn = DateTime.UtcNow,
            };
            await UserCollection.InsertOneAsync(session, user);

            return user;
        }

        [Obsolete]
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

        public Task DeleteUser(ObjectId userId) {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);

            return UserCollection.DeleteOneAsync(filter);
        }

    }
}
