﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

        protected async Task<bool> VerifyUserIsAdmin() {
            if(!User.GetUserId(out var loggedUserId)) {
                return false;
            }

            var userProfile = await UserService.GetUserById(loggedUserId);
            if(userProfile.Role == PlatformRole.Admin) {
                return true;
            }

            return false;
        }

        protected async Task<(bool Allowed, ActionResult ErrorResult, Merchant merchant, Pos pos)> VerifyUserIsUserOfPos(ObjectId posId) {
            var pos = await PosService.GetPosById(posId);
            if(pos == null) {
                Logger.LogDebug("POS {0} not found", posId);
                return (false, Problem(statusCode: StatusCodes.Status404NotFound, title: "POS not found"), null, null);
            }

            var merchant = await MerchantService.GetMerchantById(pos.MerchantId);
            if(merchant == null) {
                Logger.LogError("Owning merchant {0} for POS {1} does not exist", pos.MerchantId, pos.Id);
                return (false, Problem(statusCode: StatusCodes.Status404NotFound, title: "Owning merchant of POS not found"), null, pos);
            }

            if(!await VerifyUserIsUserOfMerchant(merchant)) {
                return (false, Problem(statusCode: StatusCodes.Status403Forbidden, title: "Logged-in user is not user of merchant"), merchant, pos);
            }

            return (true, null, merchant, pos);
        }

        protected async Task<bool> VerifyUserIsUserOfMerchant(Merchant merchant) {
            if(!User.GetUserId(out var loggedUserId)) {
                return false;
            }

            var userProfile = await UserService.GetUserById(loggedUserId);
            if(userProfile.Role == PlatformRole.Admin) {
                return true;
            }

            if(!merchant.Access.IsAtLeast(loggedUserId, MerchantRole.User)) {
                Logger.LogDebug("User {0} is not user of merchant {1}", loggedUserId, merchant.Id);
                return false;
            }

            return true;
        }

        protected async Task<(bool Allowed, ActionResult ErrorResult, Merchant merchant, Pos pos)> VerifyUserIsAdminOfPos(ObjectId posId) {
            var pos = await PosService.GetPosById(posId);
            if(pos == null) {
                Logger.LogDebug("POS {0} not found", posId);
                return (false, Problem(statusCode: StatusCodes.Status404NotFound, title: "POS not found"), null, null);
            }

            var merchant = await MerchantService.GetMerchantById(pos.MerchantId);
            if(merchant == null) {
                Logger.LogError("Owning merchant {0} for POS {1} does not exist", pos.MerchantId, pos.Id);
                return (false, Problem(statusCode: StatusCodes.Status404NotFound, title: "Owning merchant of POS not found"), null, pos);
            }

            if(!await VerifyUserIsAdminOfMerchant(merchant)) {
                return (false, Problem(statusCode: StatusCodes.Status403Forbidden, title: "Logged-in user is not administrator of merchant"), merchant, pos);
            }

            return (true, null, merchant, pos);
        }

        protected async Task<bool> VerifyUserIsAdminOfMerchant(Merchant merchant) {
            if(!User.GetUserId(out var loggedUserId)) {
                return false;
            }

            var userProfile = await UserService.GetUserById(loggedUserId);
            if(userProfile.Role == PlatformRole.Admin) {
                return true;
            }

            if(!merchant.Access.IsAtLeast(loggedUserId, MerchantRole.Admin)) {
                Logger.LogDebug("User {0} is not administrator of merchant {1}", loggedUserId, merchant.Id);
                return false;
            }

            return true;
        }

        protected async Task VerifyUserIsAdminOfSource(Source source) {
            if(!User.GetUserId(out var loggedUserId)) {
                throw ServiceProblemException.UserIsNotLoggedIn;
            }

            var userProfile = await UserService.GetUserById(loggedUserId);
            if(userProfile.Role == PlatformRole.Admin) {
                return;
            }

            if(!source.AdministratorUserIds.Contains(loggedUserId)) {
                Logger.LogDebug("User {0} is not administrator of source {1}", loggedUserId, source.Id);
                throw ServiceProblemException.UserIsNotAdminOfSource;
            }
        }

    }

}
