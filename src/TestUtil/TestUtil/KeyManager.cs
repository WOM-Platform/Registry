using Org.BouncyCastle.OpenSsl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TestUtil {

    public static class KeyManager {

        public static T LoadKeyFromPem<T>(string path) where T : class {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                using (var txReader = new StreamReader(fs)) {
                    var reader = new PemReader(txReader);
                    return reader.ReadObject() as T;
                }
            }
        }

        public static T LoadKeyFromString<T>(string pem) where T : class {
            if (string.IsNullOrWhiteSpace(pem)) {
                throw new ArgumentException("PEM cannot be empty or null", nameof(pem));
            }

            using (var sr = new StringReader(pem)) {
                var reader = new PemReader(sr);
                return reader.ReadObject() as T;
            }
        }

    }

}
