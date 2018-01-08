using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KeyUtil {

    class Program {

        static void Main(string[] args) {
            if (args.Length < 1) {
                Console.Error.WriteLine("One input word needed.");
                Environment.Exit(1);
            }

            var payloadBytes = Encoding.UTF8.GetBytes(args[0]);

            Console.WriteLine("Input: {0}", args[0]);
            Console.WriteLine("Raw: {0}, {1}", Convert.ToBase64String(payloadBytes), payloadBytes.Length);
            Console.WriteLine();

            var encryptedPayload = Encrypt("../../testkeys/source1.pem", payloadBytes);

            Console.WriteLine("Encrypted: {0}, {1} bytes", Convert.ToBase64String(encryptedPayload), encryptedPayload.Length);
            Console.WriteLine();

            var decryptedPaylod = Decrypt("../../testkeys/source1.pub", encryptedPayload);

            Console.WriteLine("Decrypted again: {0}, {1} bytes", Encoding.UTF8.GetString(decryptedPaylod), decryptedPaylod.Length);
            Console.WriteLine();

            Console.ReadKey();
        }

        private static byte[] Encrypt(string keyPath, byte[] payloadBytes) {
            var reader = new PemReader(File.OpenText(keyPath));
            var keyParam = (AsymmetricCipherKeyPair)reader.ReadObject();

            var engine = new Pkcs1Encoding(new RsaEngine());
            engine.Init(true, keyParam.Private);

            var encryptedPayload = engine.ProcessBlock(payloadBytes, 0, payloadBytes.Length);

            return encryptedPayload;
        }

        private static byte[] Decrypt(string keyPath, byte[] encryptedPayload) {
            var reader = new PemReader(File.OpenText(keyPath));
            var keyParam = (RsaKeyParameters)reader.ReadObject();

            var engine = new Pkcs1Encoding(new RsaEngine());
            engine.Init(false, keyParam);

            var decryptedPayload = engine.ProcessBlock(encryptedPayload, 0, encryptedPayload.Length);

            return decryptedPayload;
        }
    }

}
