using System;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
#if !ENABLE_TESTING
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
#endif

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

            var decryptedPaylod = Encoding.UTF8.GetString(this.DecryptBase64(payload, key));

            return JsonConvert.DeserializeObject<T>(decryptedPaylod);
        }

        public byte[] DecryptBase64(string base64Payload, AsymmetricKeyParameter key) {
            var payloadBytes = Convert.FromBase64String(base64Payload);

            var engine = new Pkcs1Encoding(new RsaEngine());
            engine.Init(false, key);
            return engine.ProcessBlock(payloadBytes, 0, payloadBytes.Length);
        }

        public string DecryptBase64AsString(string base64Payload, AsymmetricKeyParameter key) {
            return Encoding.UTF8.GetString(this.DecryptBase64(base64Payload, key));
        }

        /// <summary>
        /// Encrypts a payload by encoding in JSON and using a given private key.
        /// Result is encoded in Base64.
        /// </summary>
        /// <typeparam name="T">Type of the object to encrypt.</typeparam>
        /// <param name="payload">Object to encode in JSON and to encrypt.</param>
        /// <param name="key">Private key used for encryption.</param>
        /// <returns>Base64-encoded encrypted JSON-encoded payload.</returns>
        public string EncryptPayload<T>(T payload, AsymmetricKeyParameter key) {
            if (!key.IsPrivate) {
                throw new ArgumentException("Private key required for encryption", nameof(key));
            }

            var payloadEncoded = JsonConvert.SerializeObject(payload, Formatting.None, new JsonSerializerSettings {
                Culture = CultureInfo.InvariantCulture,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateParseHandling = DateParseHandling.DateTime,
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Include
            });
            var payloadBytes = Encoding.UTF8.GetBytes(payloadEncoded);

            return this.EncryptBytes(payloadBytes, key);
        }

        /// <summary>
        /// Encrypts a byte payload using a given key.
        /// Result is encoded in Base64.
        /// </summary>
        public string EncryptBytes(byte[] payload, AsymmetricKeyParameter key) {
            var engine = new Pkcs1Encoding(new RsaEngine());
            engine.Init(true, key);

            var encryptedBytes = engine.ProcessBlock(payload, 0, payload.Length);

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Encrypts a string payload (UTF8) using a given key.
        /// Resut is encoded in Base64.
        /// </summary>
        public string EncryptString(string payload, AsymmetricKeyParameter key) {
            return this.EncryptBytes(Encoding.UTF8.GetBytes(payload), key);
        }

    }

}
