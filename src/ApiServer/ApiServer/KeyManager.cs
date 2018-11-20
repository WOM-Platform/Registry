using System;
using System.IO;
using System.Text;
#if !ENABLE_TESTING
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
#endif
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;

namespace WomPlatform.Web.Api {

    public class KeyManager {

#if !ENABLE_TESTING

        protected readonly IConfiguration _configuration;
        protected readonly ILogger<KeyManager> _logger;

        public KeyManager(IConfiguration configuration, ILogger<KeyManager> logger) {
            this._configuration = configuration;
            this._logger = logger;

            this._logger.LogTrace(LoggingEvents.KeyManagement, "Loading registry keys");

            var keysConf = configuration.GetSection("RegistryKeys");
            if (!string.IsNullOrEmpty(keysConf["PublicPath"])) {
                this._keyPublic = LoadKeyFromPem<AsymmetricKeyParameter>(keysConf["PublicPath"]);
                this._logger.LogDebug(LoggingEvents.KeyManagement, "Public key loaded: {0}", this._keyPublic);
            }
            if (!string.IsNullOrEmpty(keysConf["PrivatePath"])) {
                this._keyPrivate = LoadKeyFromPem<AsymmetricCipherKeyPair>(keysConf["PrivatePath"]).Private;
                this._logger.LogDebug(LoggingEvents.KeyManagement, "Private key loaded: {0}", this._keyPrivate);
            }

            this._logger.LogInformation(LoggingEvents.KeyManagement, "Registry keys loaded");
        }

#endif

        public static T LoadKeyFromPem<T>(string path) where T : class {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                using(var txReader = new StreamReader(fs)) {
                    var reader = new PemReader(txReader);
                    return reader.ReadObject() as T;
                }
            }
        }

        public static T LoadKeyFromString<T>(string pem) where T : class {
            using(var sr = new StringReader(pem)) {
                var reader = new PemReader(sr);
                return reader.ReadObject() as T;
            }
        }

        private readonly AsymmetricKeyParameter _keyPrivate, _keyPublic;

        public AsymmetricKeyParameter RegistryPrivateKey {
            get {
                return this._keyPrivate;
            }
        }

        public AsymmetricKeyParameter RegistryPublicKey {
            get {
                return this._keyPublic;
            }
        }

    }

}
