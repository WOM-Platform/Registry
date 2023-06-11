using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Shared common base class for Registry controllers.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class BaseRegistryController : ControllerBase {

        protected readonly string SelfHostDomain;
        protected readonly string SelfLinkDomain;

        private readonly IServiceProvider _serviceProvider;

        public BaseRegistryController(
            IServiceProvider serviceProvider,
            ILogger<BaseRegistryController> logger
        ) {
            _serviceProvider = serviceProvider;
            Logger = logger;

            SelfHostDomain = Environment.GetEnvironmentVariable("SELF_HOST");
            SelfLinkDomain = Environment.GetEnvironmentVariable("LINK_HOST");
        }

        protected IConfiguration Configuration {
            get {
                return _serviceProvider.GetRequiredService<IConfiguration>();
            }
        }

        protected CryptoProvider Crypto {
            get {
                return _serviceProvider.GetRequiredService<CryptoProvider>();
            }
        }

        protected KeyManager KeyManager {
            get {
                return _serviceProvider.GetRequiredService<KeyManager>();
            }
        }

        protected Task<IClientSessionHandle> CreateMongoSession() {
            var client = _serviceProvider.GetRequiredService<MongoClient>();
            return client.StartSessionAsync();
        }

        protected ILogger<BaseRegistryController> Logger { get; }

        protected AimService AimService {
            get {
                return _serviceProvider.GetRequiredService<AimService>();
            }
        }

        protected ApiKeyService ApiKeyService {
            get {
                return _serviceProvider.GetRequiredService<ApiKeyService>();
            }
        }

        protected GenerationService GenerationService {
            get {
                return _serviceProvider.GetRequiredService<GenerationService>();
            }
        }

        protected MerchantService MerchantService {
            get {
                return _serviceProvider.GetRequiredService<MerchantService>();
            }
        }

        protected OfferService OfferService {
            get {
                return _serviceProvider.GetRequiredService<OfferService>();
            }
        }

        protected PaymentService PaymentService {
            get {
                return _serviceProvider.GetRequiredService<PaymentService>();
            }
        }

        protected PicturesService PicturesService {
            get {
                return _serviceProvider.GetRequiredService<PicturesService>();
            }
        }

        protected PosService PosService {
            get {
                return _serviceProvider.GetRequiredService<PosService>();
            }
        }

        protected SourceService SourceService {
            get {
                return _serviceProvider.GetRequiredService<SourceService>();
            }
        }

        protected StatsService StatsService {
            get {
                return _serviceProvider.GetRequiredService<StatsService>();
            }
        }

        protected UserService UserService {
            get {
                return _serviceProvider.GetRequiredService<UserService>();
            }
        }

        protected (T, ActionResult) ExtractInputPayload<T>(string payload, int loggingKey) {
            try {
                return (Crypto.Decrypt<T>(payload, KeyManager.RegistryPrivateKey), null);
            }
            catch(Exception ex) {
                Logger.LogError(loggingKey, ex, "Failed to decrypt payload");
                return (default(T), ControllerExtensions.PayloadVerificationFailure(null));
            }
        }

        /// <summary>
        /// Check whether a user password is acceptable.
        /// </summary>
        protected bool CheckUserPassword(string password) {
            var userSecuritySection = Configuration.GetSection("UserSecurity");
            var minLength = Convert.ToInt32(userSecuritySection["MinimumUserPasswordLength"]);

            if(string.IsNullOrWhiteSpace(password)) {
                return false;
            }

            if(password.Length < minLength) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether a given voucher transfer password is acceptable.
        /// Null passwords are accepted as auto-generated.
        /// </summary>
        protected bool CheckTransferPassword(string password, bool acceptNullPasswords = true) {
            if(password == null && acceptNullPasswords) {
                // Null passwords will be autogenerated
                return true;
            }

            var confSection = Configuration.GetSection("PasswordSecurity");
            var minLength = Convert.ToInt32(confSection["MinimumLength"]);
            if(password.Length < minLength) {
                return false;
            }

            return true;
        }

        private async Task<(User UserProfile, bool IsAdmin)> CheckLoggedInUser() {
            if(!User.GetUserId(out var loggedUserId)) {
                throw ServiceProblemException.UserIsNotLoggedIn;
            }

            var userProfile = await UserService.GetUserById(loggedUserId);
            if(userProfile == null) {
                throw ServiceProblemException.UserProfileDoesNotExist;
            }

            return (userProfile, userProfile.Role == PlatformRole.Admin);
        }

        /// <summary>
        /// Verifies that the logged-in user is a platform administrator.
        /// Returns the logged-in user's profile.
        /// </summary>
        protected async Task<User> VerifyUserIsAdmin() {
            (var userProfile, bool isAdmin) = await CheckLoggedInUser();
            if(!isAdmin) {
                throw ServiceProblemException.UserIsNotAdmin;
            }

            return userProfile;
        }

        /// <summary>
        /// Verifies that the logged-in user is an authorized POS user.
        /// </summary>
        protected async Task<(Merchant merchant, Pos pos)> VerifyUserIsUserOfPos(ObjectId posId) {
            (var userProfile, bool isAdmin) = await CheckLoggedInUser();

            var pos = await PosService.GetPosById(posId);
            if(pos == null) {
                Logger.LogDebug("POS {0} not found", posId);
                throw ServiceProblemException.PosNotFound;
            }

            var merchant = await MerchantService.GetMerchantById(pos.MerchantId);
            if(merchant == null) {
                Logger.LogError("Owning merchant {0} for POS {1} does not exist", pos.MerchantId, pos.Id);
                throw ServiceProblemException.OwningMerchantOfPosNotFound;
            }

            if(!isAdmin) {
                if(!merchant.Access.IsAtLeast(userProfile.Id, MerchantRole.User)) {
                    Logger.LogDebug("User {0} is not user of merchant {1}", userProfile.Id, merchant.Id);
                    throw ServiceProblemException.UserIsNotUserOfMerchant;
                }
            }

            return (merchant, pos);
        }

        /// <summary>
        /// Verifies that the logged-in user is an authorized merchant user.
        /// </summary>
        protected async Task<Merchant> VerifyUserIsUserOfMerchant(ObjectId merchantId) {
            (var userProfile, bool isAdmin) = await CheckLoggedInUser();

            var merchant = await MerchantService.GetMerchantById(merchantId);
            if(merchant == null) {
                Logger.LogDebug("Merchant {0} not found", merchantId);
                throw ServiceProblemException.MerchantNotFound;
            }

            if(!isAdmin) {
                if(!merchant.Access.IsAtLeast(userProfile.Id, MerchantRole.User)) {
                    Logger.LogDebug("User {0} is not user of merchant {1}", userProfile.Id, merchant.Id);
                    throw ServiceProblemException.UserIsNotUserOfMerchant;
                }
            }

            return merchant;
        }

        /// <summary>
        /// Verifies that the logged-in user is an authorized POS administrator.
        /// </summary>
        protected async Task<(Merchant merchant, Pos pos)> VerifyUserIsAdminOfPos(ObjectId posId) {
            (var userProfile, bool isAdmin) = await CheckLoggedInUser();

            var pos = await PosService.GetPosById(posId);
            if(pos == null) {
                Logger.LogDebug("POS {0} not found", posId);
                throw ServiceProblemException.PosNotFound;
            }

            var merchant = await MerchantService.GetMerchantById(pos.MerchantId);
            if(merchant == null) {
                Logger.LogError("Owning merchant {0} for POS {1} does not exist", pos.MerchantId, pos.Id);
                throw ServiceProblemException.OwningMerchantOfPosNotFound;
            }

            if(!isAdmin) {
                if(!merchant.Access.IsAtLeast(userProfile.Id, MerchantRole.Admin)) {
                    Logger.LogDebug("User {0} is not user of merchant {1}", userProfile.Id, merchant.Id);
                    throw ServiceProblemException.UserIsNotUserOfMerchant;
                }
            }

            return (merchant, pos);
        }

        /// <summary>
        /// Verifies that the logged-in user is an authorized merchant administrator.
        /// </summary>
        protected async Task<Merchant> VerifyUserIsAdminOfMerchant(ObjectId merchantId) {
            (var userProfile, bool isAdmin) = await CheckLoggedInUser();

            var merchant = await MerchantService.GetMerchantById(merchantId);
            if(merchant == null) {
                Logger.LogDebug("Merchant {0} not found", merchantId);
                throw ServiceProblemException.MerchantNotFound;
            }

            if(!isAdmin) {
                if(!merchant.Access.IsAtLeast(userProfile.Id, MerchantRole.Admin)) {
                    Logger.LogDebug("User {0} is not user of merchant {1}", userProfile.Id, merchant.Id);
                    throw ServiceProblemException.UserIsNotUserOfMerchant;
                }
            }

            return merchant;
        }

        protected async Task<Source> VerifyUserIsAdminOfSource(ObjectId sourceId) {
            (var userProfile, bool isAdmin) = await CheckLoggedInUser();

            var source = await SourceService.GetSourceById(sourceId);
            if(source == null) {
                Logger.LogDebug("Source {0} not found", sourceId);
                throw ServiceProblemException.SourceNotFound;
            }

            if(!isAdmin) {
                if(!source.AdministratorUserIds.Contains(userProfile.Id)) {
                    Logger.LogDebug("User {0} is not administrator of source {1}", userProfile.Id, sourceId);
                    throw ServiceProblemException.UserIsNotAdminOfSource;
                }
            }

            return source;
        }

    }

}
