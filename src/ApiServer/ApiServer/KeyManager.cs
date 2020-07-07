using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace WomPlatform.Web.Api {

    public class KeyManager {

        protected IConfiguration Configuration { get; }
        protected ILogger<KeyManager> Logger { get; }

        public KeyManager(
            IConfiguration configuration,
            ILogger<KeyManager> logger
        ) {
            Configuration = configuration;
            Logger = logger;

            Logger.LogTrace(LoggingEvents.KeyManagement, "Loading registry keys");

            var keysConf = configuration.GetSection("RegistryKeys");
            if (!string.IsNullOrEmpty(keysConf["PublicPath"])) {
                RegistryPublicKey = CryptoHelper.LoadKeyFromPem<AsymmetricKeyParameter>(keysConf["PublicPath"]);
                Logger.LogDebug(LoggingEvents.KeyManagement, "Public key loaded: {0}", RegistryPublicKey);
            }
            if (!string.IsNullOrEmpty(keysConf["PrivatePath"])) {
                RegistryPrivateKey = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>(keysConf["PrivatePath"]).Private;
                Logger.LogDebug(LoggingEvents.KeyManagement, "Private key loaded: {0}", RegistryPrivateKey);
            }

            Logger.LogTrace(LoggingEvents.KeyManagement, "Registry keys loaded");
        }

        public AsymmetricKeyParameter RegistryPrivateKey { get; }

        public AsymmetricKeyParameter RegistryPublicKey { get; }

    }

}
