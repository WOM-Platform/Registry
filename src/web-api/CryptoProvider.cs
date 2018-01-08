#if !ENABLE_TESTING
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
#endif
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using System;
using System.Globalization;
using System.Text;

namespace WomPlatform.Web.Api {

    /// <summary>
    /// Provides access to auxiliary high-level cryptographic functions.
    /// </summary>
    public class CryptoProvider {

#if !ENABLE_TESTING

        protected readonly IConfiguration _configuration;
        protected readonly KeyManager _keyManager;
        protected readonly ILogger<CryptoProvider> _logger;

        public CryptoProvider(IConfiguration configuration, KeyManager keyManager, ILogger<CryptoProvider> logger) {
            this._configuration = configuration;
            this._keyManager = keyManager;
            this._logger = logger;
        }

#else

        public CryptoProvider() {

        }

#endif

        /// <summary>
        /// Attempts to decrypt an encrypted JSON payload (encoded in base64) and interprets it as a JSON object.
        /// </summary>
        /// <typeparam name="T">Type of the decrypted JSON object.</typeparam>
        /// <param name="payload">Encrypted and base64-encoded input payload.</param>
        /// <param name="key">Public key used for decryption.</param>
        public T DecryptPayload<T>(string payload, AsymmetricKeyParameter key) {
            if(key.IsPrivate) {
                throw new ArgumentException("Public key required for decryption", nameof(key));
            }

            var payloadBytes = Convert.FromBase64String(payload);

            var engine = new Pkcs1Encoding(new RsaEngine());
            engine.Init(false, key);

            var decryptedBytes = engine.ProcessBlock(payloadBytes, 0, payloadBytes.Length);
            var decryptedPaylod = Encoding.UTF8.GetString(decryptedBytes);

            return JsonConvert.DeserializeObject<T>(decryptedPaylod);
        }

        /// <summary>
        /// Encrypts a payload by encoding in JSON and using a given private key.
        /// </summary>
        /// <typeparam name="T">Type of the object to encrypt.</typeparam>
        /// <param name="payload">Object to encode in JSON and to encrypt.</param>
        /// <param name="key">Private key used for encryption.</param>
        /// <returns>Base64-encoded encrypted JSON-encoded payload.</returns>
        public string EncryptPayload<T>(T payload, AsymmetricKeyParameter key) {
            if (!key.IsPrivate) {
                throw new ArgumentException("Private key required for decryption", nameof(key));
            }

            var payloadEncoded = JsonConvert.SerializeObject(payload, Formatting.None, new JsonSerializerSettings {
                Culture = CultureInfo.InvariantCulture,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateParseHandling = DateParseHandling.DateTime,
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Include
            });

            var engine = new Pkcs1Encoding(new RsaEngine());
            engine.Init(true, key);

            var payloadBytes = Encoding.UTF8.GetBytes(payloadEncoded);
            var encryptedBytes = engine.ProcessBlock(payloadBytes, 0, payloadBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }

    }

}
