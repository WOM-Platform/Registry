using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;

namespace WomPlatform.Web.Api {

    public class KeyManager {

        protected readonly IConfiguration _configuration;
        protected readonly ILogger<KeyManager> _logger;

        public KeyManager(IConfiguration configuration, ILogger<KeyManager> logger) {
            this._configuration = configuration;
            this._logger = logger;

            this._logger.LogTrace(LoggingEvents.KeyManagement, "Loading registry keys");

            var keysConf = configuration.GetSection("RegistryKeys");
            if (!string.IsNullOrEmpty(keysConf["PublicPath"])) {
                this.RegistryPublicKey = LoadKeyFromPem<AsymmetricKeyParameter>(keysConf["PublicPath"]);
                this._logger.LogDebug(LoggingEvents.KeyManagement, "Public key loaded: {0}", this.RegistryPublicKey);
            }
            if (!string.IsNullOrEmpty(keysConf["PrivatePath"])) {
                this.RegistryPrivateKey = LoadKeyFromPem<AsymmetricCipherKeyPair>(keysConf["PrivatePath"]).Private;
                this._logger.LogDebug(LoggingEvents.KeyManagement, "Private key loaded: {0}", this.RegistryPrivateKey);
            }

            this._logger.LogInformation(LoggingEvents.KeyManagement, "Registry keys loaded");
        }

        public static T LoadKeyFromPem<T>(string path) where T : class {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                using(var txReader = new StreamReader(fs)) {
                    var reader = new PemReader(txReader);
                    return reader.ReadObject() as T;
                }
            }
        }

        public static T LoadKeyFromString<T>(string pem) where T : class {
            if(string.IsNullOrWhiteSpace(pem)) {
                throw new ArgumentException("PEM cannot be empty or null", nameof(pem));
            }

            using(var sr = new StringReader(pem)) {
                var reader = new PemReader(sr);
                return reader.ReadObject() as T;
            }
        }

        public AsymmetricKeyParameter RegistryPrivateKey { get; }

        public AsymmetricKeyParameter RegistryPublicKey { get; }

    }

}
