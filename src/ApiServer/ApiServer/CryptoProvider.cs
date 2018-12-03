using System;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;

namespace WomPlatform.Web.Api {

    /// <summary>
    /// Provides access to auxiliary high-level cryptographic functions.
    /// </summary>
    public class CryptoProvider {

        public CryptoProvider(ILogger<CryptoProvider> logger) {
            Logger = logger;
        }

        protected ILogger<CryptoProvider> Logger { get; }

        private readonly static JsonSerializerSettings JsonSettings = new JsonSerializerSettings {
            Culture = CultureInfo.InvariantCulture,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            DateParseHandling = DateParseHandling.DateTime,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Include
        };

        /// <summary>
        /// Decrypts a byte payload using a given key.
        /// </summary>
        public byte[] Decrypt(byte[] payload, AsymmetricKeyParameter key) {
            var engine = new Pkcs1Encoding(new RsaEngine());
            engine.Init(false, key);

            int inBlockSize = engine.GetInputBlockSize();
            int outBlockSize = engine.GetOutputBlockSize();
            int blocks = (int)Math.Ceiling(payload.Length / (double)inBlockSize);
            byte[] output = new byte[blocks * outBlockSize];
            for (int i = 0; i < blocks; ++i) {
                int offset = i * inBlockSize;
                var cryptoBlock = engine.ProcessBlock(payload, offset, Math.Min(inBlockSize, payload.Length - offset));
                cryptoBlock.CopyTo(output, i * outBlockSize);
            }

            Logger.LogTrace("Decrypt {0} bytes ({1} blocks {2} to {3} bytes), output {4} bytes",
                payload.Length, blocks, inBlockSize, outBlockSize, output.Length);

            return output;
        }

        /// <summary>
        /// Encrypts a byte payload using a given key.
        /// </summary>
        public byte[] Encrypt(byte[] payload, AsymmetricKeyParameter key) {
            var engine = new Pkcs1Encoding(new RsaEngine());
            engine.Init(true, key);

            int inBlockSize = engine.GetInputBlockSize();
            int outBlockSize = engine.GetOutputBlockSize();
            int blocks = (int)Math.Ceiling(payload.Length / (double)inBlockSize);
            byte[] output = new byte[blocks * outBlockSize];
            for (int i = 0; i < blocks; ++i) {
                int offset = i * inBlockSize;
                var cryptoBlock = engine.ProcessBlock(payload, offset, Math.Min(inBlockSize, payload.Length - offset));
                cryptoBlock.CopyTo(output, i * outBlockSize);
            }

            Logger.LogTrace("Encrypt {0} bytes ({1} blocks {2} to {3} bytes), output {4} bytes",
                payload.Length, blocks, inBlockSize, outBlockSize, output.Length);

            return output;
        }

        /// <summary>
        /// Decrypts a base64 payload and verifies it.
        /// If successful, the data is parsed as an UTF8 JSON string.
        /// </summary>
        /// <typeparam name="T">Type of the object to decrypt.</typeparam>
        public T DecryptAndVerify<T>(string payload,
            AsymmetricKeyParameter senderPublicKey, AsymmetricKeyParameter receiverPrivateKey) {
            if(senderPublicKey.IsPrivate) {
                throw new ArgumentException("Public key of sender required for verification", nameof(senderPublicKey));
            }
            if(!receiverPrivateKey.IsPrivate) {
                throw new ArgumentException("Private key of receiver required for decryption", nameof(receiverPrivateKey));
            }

            var payloadBytes = payload.FromBase64();
            var decryptBytes = Decrypt(payloadBytes, receiverPrivateKey);
            var verifyBytes = Decrypt(decryptBytes, senderPublicKey);

            Logger.LogTrace("Decrypt and verify {3} chars (bytes {0} => {1} => {2})",
                payloadBytes.Length, decryptBytes.Length, verifyBytes.Length, payload.Length);

            return JsonConvert.DeserializeObject<T>(verifyBytes.AsUtf8String(), JsonSettings);
        }

        /// <summary>
        /// Signs and encrypts an object payload.
        /// If successful, payload is encoded as a base64 string.
        /// </summary>
        /// <typeparam name="T">Type of the object to sign and encrypt.</typeparam>
        public string SignAndEncrypt<T>(T payload,
            AsymmetricKeyParameter senderPrivateKey, AsymmetricKeyParameter receiverPublicKey) {
            if (!senderPrivateKey.IsPrivate) {
                throw new ArgumentException("Private key of sender required for signing", nameof(senderPrivateKey));
            }
            if (receiverPublicKey.IsPrivate) {
                throw new ArgumentException("Public key of receiver required for encryption", nameof(receiverPublicKey));
            }

            var payloadBytes = JsonConvert.SerializeObject(payload, JsonSettings).ToBytes();
            var signedBytes = Encrypt(payloadBytes, senderPrivateKey);
            var encryptedBytes = Encrypt(signedBytes, receiverPublicKey);

            Logger.LogTrace("Sign and encrypt object (bytes {0} => {1} => {2})",
                payloadBytes.Length, signedBytes.Length, encryptedBytes.Length);

            return encryptedBytes.ToBase64();
        }

        /// <summary>
        /// Signs an object payload.
        /// If successful, payload is encoded as a base64 string.
        /// </summary>
        /// <typeparam name="T">Type of the object to sign.</typeparam>
        public string Sign<T>(T payload, AsymmetricKeyParameter senderPrivateKey) {
            if (!senderPrivateKey.IsPrivate) {
                throw new ArgumentException("Private key of sender required for signing", nameof(senderPrivateKey));
            }

            var payloadBytes = JsonConvert.SerializeObject(payload, JsonSettings).ToBytes();
            var signedBytes = Encrypt(payloadBytes, senderPrivateKey);

            Logger.LogTrace("Sign object (bytes {0} => {1})",
                payloadBytes.Length, signedBytes.Length);

            return signedBytes.ToBase64();
        }

        /// <summary>
        /// Verifies a base64 payload.
        /// If successful, the data is parsed as an UTF8 JSON string.
        /// </summary>
        /// <typeparam name="T">Type of the object to decrypt.</typeparam>
        public T Verify<T>(string payload, AsymmetricKeyParameter senderPublicKey) {
            if (senderPublicKey.IsPrivate) {
                throw new ArgumentException("Public key of sender required for verification", nameof(senderPublicKey));
            }

            var payloadBytes = payload.FromBase64();
            var verifyBytes = Decrypt(payloadBytes, senderPublicKey);

            Logger.LogTrace("Verify {2} chars (bytes {0} => {1})",
                payloadBytes.Length, verifyBytes.Length, payload.Length);

            return JsonConvert.DeserializeObject<T>(verifyBytes.AsUtf8String(), JsonSettings);
        }

    }

}
