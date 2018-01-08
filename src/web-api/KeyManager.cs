using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api {

    public class KeyManager {

        private readonly byte[] _keyPrivate, _keyPublic;

        private const string EndMarker = "-----END";

        protected readonly IConfiguration _configuration;
        protected readonly ILogger<KeyManager> _logger;

        public KeyManager(IConfiguration configuration, ILogger<KeyManager> logger) {
            this._configuration = configuration;
            this._logger = logger;

            this._logger.LogTrace(LoggingEvents.KeyManagement, "Loading registry keys");

            var keysConf = configuration.GetSection("RegistryKeys");
            if (!string.IsNullOrEmpty(keysConf.["PublicPath"])) {
                PreloadKey(keysConf["PublicPath"], "-----BEGIN PUBLIC KEY-----", ref this._keyPublic);
                this._logger.LogDebug(LoggingEvents.KeyManagement, "Public key loaded, {0} bytes", this._keyPublic.Length);
            }
            if (!string.IsNullOrEmpty(keysConf.["PrivatePath"])) {
                PreloadKey(keysConf["PrivatePath"], "-----BEGIN RSA PRIVATE KEY-----", ref this._keyPrivate);
                this._logger.LogDebug(LoggingEvents.KeyManagement, "Private key loaded, {0} bytes", this._keyPrivate.Length);
            }

            this._logger.LogInformation(LoggingEvents.KeyManagement, "Registry keys loaded");
        }

        private void PreloadKey(string path, string expectedHeader, ref byte[] destination) {
            using(var reader = new StreamReader(path, Encoding.UTF8)) {
                if(reader.ReadLine() != expectedHeader) {
                    throw new ArgumentException("Key file does not start with expected header", nameof(path));
                }

                var buffer = new StringBuilder();
                string line = reader.ReadLine();
                while(line != null && !line.StartsWith(EndMarker)) {
                    buffer.Append(line);
                    line = reader.ReadLine();
                }

                destination = Convert.FromBase64String(buffer.ToString());
            }
        }

        public byte[] PrivateKey {
            get {
                return this._keyPrivate;
            }
        }

        public byte[] PublicKey {
            get {
                return this._keyPublic;
            }
        }

    }

}
