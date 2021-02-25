﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Shared common base class for Registry controllers.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class BaseRegistryController : ControllerBase {

        protected readonly string SelfHostDomain;

        public BaseRegistryController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<BaseRegistryController> logger
        ) {
            Configuration = configuration;
            Crypto = crypto;
            KeyManager = keyManager;
            Mongo = mongo;
            Operator = @operator;
            Logger = logger;

            SelfHostDomain = Environment.GetEnvironmentVariable("SELF_HOST");
        }

        protected IConfiguration Configuration { get; }

        protected CryptoProvider Crypto { get; }

        protected KeyManager KeyManager { get; }

        protected MongoDatabase Mongo { get; }

        protected Operator Operator { get; }

        protected ILogger<BaseRegistryController> Logger { get; }

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
        /// Checks whether a given voucher transfer password is acceptable.
        /// Null passwords are accepted as auto-generated.
        /// </summary>
        protected bool CheckTransferPassword(string password) {
            if(password == null) {
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

    }

}
