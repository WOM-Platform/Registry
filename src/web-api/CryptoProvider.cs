using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography;
using System.Text;

namespace WomPlatform.Web.Api {

    public class CryptoProvider {

        protected readonly IConfiguration _configuration;
        protected readonly KeyManager _keyManager;

        public CryptoProvider(IConfiguration configuration, KeyManager keyManager) {
            this._configuration = configuration;
            this._keyManager = keyManager;
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
