using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Shared common base class for Registry controllers.
    /// </summary>
    public class BaseRegistryController : Controller {

        public BaseRegistryController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            DatabaseOperator database,
            ILogger<BaseRegistryController> logger
        ) {
            Configuration = configuration;
            Crypto = crypto;
            KeyManager = keyManager;
            Database = database;
            Logger = logger;
        }

        protected IConfiguration Configuration { get; }

        protected CryptoProvider Crypto { get; }

        protected KeyManager KeyManager { get; }

        protected DatabaseOperator Database { get; }

        protected ILogger<BaseRegistryController> Logger { get; }

        protected (T, ActionResult) ExtractInputPayload<T>(string payload, int loggingKey) {
            try {
                return (Crypto.Decrypt<T>(payload, KeyManager.RegistryPrivateKey), null);
            }
            catch(JsonSerializationException jsonEx) {
                Logger.LogError(loggingKey, jsonEx, "Failed to decode JSON payload");
                return (default(T), ControllerExtensions.ProblemParameter(null, "Invalid JSON"));
            }
            catch(Exception ex) {
                Logger.LogError(loggingKey, ex, "Failed to decrypt payload");
                return (default(T), ControllerExtensions.PayloadVerificationFailure(null));
            }
        }

    }

}
