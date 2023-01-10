using System;
using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace WomPlatform.Web.Api {

    public static class CryptoHelper {

        public static T LoadKeyFromPem<T>(string path) where T : class {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var txReader = new StreamReader(fs);
            var reader = new PemReader(txReader);
            return reader.ReadObject() as T;
        }

        public static T LoadKeyFromString<T>(string pem) where T : class {
            if(string.IsNullOrWhiteSpace(pem)) {
                throw new ArgumentException("PEM cannot be empty or null", nameof(pem));
            }

            using var sr = new StringReader(pem);
            var reader = new PemReader(sr);
            return reader.ReadObject() as T;
        }

        public static string ToPemString(this AsymmetricKeyParameter keyParameter) {
            using var stringWriter = new StringWriter();
            var writer = new PemWriter(stringWriter);
            writer.WriteObject(keyParameter);
            stringWriter.Flush();

            return stringWriter.GetStringBuilder().ToString();
        }

        public static AsymmetricCipherKeyPair CreateKeyPair() {
            return DotNetUtilities.GetRsaKeyPair(RSA.Create());
        }

    }

}
