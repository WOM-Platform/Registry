using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace WomPlatform.Web.Api {

    public class KeyManager {

        private readonly byte[] _keyPrivate, _keyPublic;

        private const string EndMarker = "-----END";

        protected readonly IConfiguration _configuration;

        public KeyManager(IConfiguration configuration) {
            this._configuration = configuration;

            var keysConf = configuration.GetSection("RegistryKeys");
            PreloadKey(keysConf["PublicPath"], "-----BEGIN PUBLIC KEY-----", ref this._keyPublic);
            PreloadKey(keysConf["PrivatePath"], "-----BEGIN RSA PRIVATE KEY-----", ref this._keyPrivate);
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
