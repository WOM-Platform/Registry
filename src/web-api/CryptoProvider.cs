using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;

namespace WomPlatform.Web.Api {

    public class CryptoProvider {

        protected readonly IConfiguration _configuration;
        protected readonly KeyManager _keyManager;
        protected readonly ILogger<CryptoProvider> _logger;

        public CryptoProvider(IConfiguration configuration, KeyManager keyManager, ILogger<CryptoProvider> logger) {
            this._configuration = configuration;
            this._keyManager = keyManager;
            this._logger = logger;
        }

        public const int KeyBits = 4096;

        public string Decrypt(string payload, string key) {
            var keyBytes = Convert.FromBase64String(key);
            var payloadBytes = Convert.FromBase64String(payload);

            var provider = new RSACryptoServiceProvider(KeyBits);
            provider.ImportCspBlob(keyBytes);

            return Encoding.UTF8.GetString(provider.Decrypt(payloadBytes, RSAEncryptionPadding.Pkcs1));
        }

    }

}
