using System;
using System.Security.Cryptography;
using System.Text;

namespace KeyUtil {

    class Program {

        const int KeyBits = 4096;

        static void Main(string[] args) {
            Console.WriteLine("Original: {0}", args[0]);

            var crypto1 = new RSACryptoServiceProvider(KeyBits);
            Console.WriteLine(crypto1.ToString());
            Console.WriteLine("Algo '{0}', key size {1}, public only {2}", crypto1.KeyExchangeAlgorithm, crypto1.KeySize, crypto1.PublicOnly);

            var cspBytes = crypto1.ExportCspBlob(true);
            Console.WriteLine("CSP: {0} ({1} bytes)", Convert.ToBase64String(cspBytes), cspBytes.Length);

            var crypto2 = new RSACryptoServiceProvider(KeyBits);
            crypto2.ImportCspBlob(cspBytes);
            Console.WriteLine("Algo '{0}', key size {1}, public only {2}", crypto2.KeyExchangeAlgorithm, crypto2.KeySize, crypto2.PublicOnly);

            var encrypted = crypto1.Encrypt(Encoding.UTF8.GetBytes(args[0]), RSAEncryptionPadding.Pkcs1);
            Console.WriteLine("Encrypted: {0} ({1} bytes)", Convert.ToBase64String(encrypted), encrypted.Length);

            var decrypted = crypto2.Decrypt(encrypted, RSAEncryptionPadding.Pkcs1);
            Console.WriteLine("Decrypted: {0}", Encoding.UTF8.GetString(decrypted));
        }

    }

}
