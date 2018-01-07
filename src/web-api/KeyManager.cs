using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public class KeyManager {

        private readonly byte[] _keyPrivate, _keyPublic;

        private const string EndMarker = "-----END";

        public KeyManager(IConfiguration configuration) {
            var keysConf = configuration.GetSection("RegistryKeys");

            PreloadKey(keysConf["PublicPath"], "-----BEGIN PUBLIC KEY-----", ref _keyPublic);
            PreloadKey(keysConf["PrivatePath"], "-----BEGIN RSA PRIVATE KEY-----", ref _keyPrivate);
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
                return _keyPrivate;
            }
        }

        public byte[] PublicKey {
            get {
                return _keyPublic;
            }
        }

    }

}
