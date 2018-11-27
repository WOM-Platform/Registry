using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using RestSharp;
using System;
using System.IO;
using System.Linq;
using System.Text;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.Models;

namespace TestUtil {

    class Program {

        public static string Host { get; private set; }
        public static int Port { get; private set; }

        private static RestClient Client {
            get {
                return new RestClient($"http://{Host}:{Port}/api/v1");
            }
        }

        public static void Main(string[] args) {
            if(args.Length < 3) {
                Console.Error.WriteLine("Usage: TestUtil <host> <port> command");
                Environment.Exit(1);
            }

            Host = args[0];
            Port = int.Parse(args[1]);

            switch(args[2]) {
                case "vcreate":
                    CreateVouchers(args.Skip(3).ToArray());
                    break;

                default:
                    Console.Error.WriteLine("Unknown command");
                    Environment.Exit(1);
                    break;
            }
        }

        private static void CreateVouchers(string[] args) {
            var privateKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("../../../testkeys/source1.pem").Private;
            var crypto = new CryptoProvider();

            var nonce = Guid.NewGuid();

            var request = new RestRequest("voucher/create", Method.POST) {
                RequestFormat = DataFormat.Json
            };
            request.AddHeader("Accept", "application/json");
            request.AddJsonBody(new VoucherCreatePayload {
                SourceId = 1,
                Nonce = nonce,
                Payload = crypto.EncryptPayload(new VoucherCreatePayloadContent {
                    Nonce = nonce,
                    SourceId = 1,
                    Vouchers = new VoucherCreatePayloadContent.VoucherInfo[] {

                    }
                }, privateKey)
            });

            var response = Client.Execute<VoucherCreateResponse>(request);
            Console.WriteLine("HTTP {0}, {1} bytes, {2}", response.StatusCode, response.ContentLength, response.ContentType);
            Console.WriteLine("Response: {0}", response.Content);

            var decryptedOtc = crypto.DecryptBase64AsString(response.Data.EncryptedOtc, privateKey);
            Console.WriteLine("OTC: {0}", decryptedOtc);
        }

    }

}
