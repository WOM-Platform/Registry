using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.Mail;

namespace WomPlatform.Web.Api.Service {
    public class UserService : BaseService {

        private readonly MongoClient _client;
        private readonly MailerComposer _composer;
        private readonly ILogger<UserService> _logger;

        public UserService(
            MongoClient client,
            MailerComposer composer,
            ILogger<UserService> logger
        ) : base(client, logger) {
            _client = client;
            _composer = composer;
            _logger = logger;
        }

        public Task<User> GetUserById(ObjectId id) {
            var filter = Builders<User>.Filter.Eq(u => u.Id, id);
            return UserCollection.Find(filter).SingleOrDefaultAsync();
        }

        public Task<User> GetUserByEmail(string email) {
            if(string.IsNullOrWhiteSpace(email)) {
                throw new ArgumentNullException(nameof(email));
            }

            var filter = Builders<User>.Filter.Eq(u => u.Email, email.Trim());
            var options = new FindOptions {
                Collation = new Collation("en", strength: CollationStrength.Secondary, caseLevel: false)
            };
            return UserCollection.Find(filter, options).SingleOrDefaultAsync();
        }

        public async Task<User> CreateUser(
            IClientSessionHandle session,
            string email, string name, string surname, string password,
            bool isVerified = false,
            PlatformRole platformRole = PlatformRole.User
        ) {
            var effectiveEmail = email.Trim();

            var existingUser = await GetUserByEmail(effectiveEmail);
            if(existingUser != null) {
                throw ServiceProblemException.EmailAlreadyRegistered;
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

            _composer.SendVerificationMail(user);

            return user;
        }

        public Task<User> UpdateUser(
            ObjectId userId,
            string name = null,
            string surname = null,
            string email = null,
            string password = null
        ) {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);

            var chain = Builders<User>.Update.Chain();
            if(name != null)
                chain.Set(u => u.Name, name);
            if(surname != null)
                chain.Set(u => u.Surname, surname);
            if(email != null)
                chain.Set(u => u.Email, email);
            if(password != null)
                chain.Set(u => u.PasswordHash, BCrypt.Net.BCrypt.HashPassword(password));
            chain.Set(u => u.LastUpdate, DateTime.UtcNow);

            return UserCollection.FindOneAndUpdateAsync(filter, chain.End(), new FindOneAndUpdateOptions<User, User> { ReturnDocument = ReturnDocument.After });
        }

        /// <summary>
        /// Requests a new verification e-mail (will be sent if the user is not already verified).
        /// </summary>
        public bool RequestVerificationEmail(User user) {
            if(user.VerificationToken != null) {
                _composer.SendVerificationMail(user);
                return true;
            }
            else {
                return false;
            }
        }

        /// <summary>
        /// Attempts to verify a user.
        /// </summary>
        /// <param name="userId">User ID to match.</param>
        /// <param name="verificationToken">Verification token to match.</param>
        public async Task PerformVerification(ObjectId userId, string verificationToken) {
            var user = await UserCollection.Find(Builders<User>.Filter.Eq(u => u.Id, userId)).SingleOrDefaultAsync();
            if(user == null) {
                throw ServiceProblemException.UserNotFound;
            }
            if(user.VerificationToken == null) {
                return;
            }

            if(!user.VerificationToken.Equals(verificationToken, StringComparison.InvariantCultureIgnoreCase)) {
                throw ServiceProblemException.TokenNotValid;
            }

            var results = await UserCollection.UpdateOneAsync(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Update.Set(u => u.VerificationToken, null)
            );
            if(results.ModifiedCount != 1) {
                throw new InvalidOperationException($"Set verification token operation modified {results.ModifiedCount} records instead of 1");
            }
        }

        /// <summary>
        /// Request a new password request token.
        /// </summary>
        public async Task<User> RequestPasswordReset(string email, bool sendNotification = true) {
            var user = await UserCollection.FindOneAndUpdateAsync(
                Builders<User>.Filter.Eq(u => u.Email, email.Trim()),
                Builders<User>.Update.Set(u => u.PasswordResetToken, Random.GenerateReadableCode(8)),
                new FindOneAndUpdateOptions<User> {
                    Collation = new Collation("en", strength: CollationStrength.Secondary, caseLevel: false),
                    ReturnDocument = ReturnDocument.After,
                }
            );
            if(user == null) {
                throw ServiceProblemException.UserNotFound;
            }

            if(sendNotification) {
                _composer.SendPasswordResetMail(user);
            }

            return user;
        }

        public enum TokenVerification {
            Normal,
            Skip
        }

        /// <summary>
        /// Attempts to perform a password reset.
        /// </summary>
        public async Task PerformPasswordReset(ObjectId userId, string passwordResetToken, string newPassword, TokenVerification verification = TokenVerification.Normal) {
            if(string.IsNullOrWhiteSpace(newPassword)) {
                throw new ArgumentNullException(nameof(newPassword));
            }

            var user = await UserCollection.Find(Builders<User>.Filter.Eq(u => u.Id, userId)).SingleOrDefaultAsync();
            if(user == null) {
                throw ServiceProblemException.UserNotFound;
            }

            if(verification != TokenVerification.Skip) {
                if(!passwordResetToken.Equals(user.PasswordResetToken, StringComparison.InvariantCultureIgnoreCase)) {
                    throw ServiceProblemException.TokenNotValid;
                }
            }

            var results = await UserCollection.UpdateOneAsync(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Update
                    .Set(u => u.PasswordHash, BCrypt.Net.BCrypt.HashPassword(newPassword))
                    .Set(u => u.PasswordResetToken, null)
            );
            if(results.ModifiedCount != 1) {
                throw new InvalidOperationException($"Set new password operation modified {results.ModifiedCount} records instead of 1");
            }
        }

        public Task DeleteUser(ObjectId userId) {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);

            return UserCollection.DeleteOneAsync(filter);
        }

    }
}
