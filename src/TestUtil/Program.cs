using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using RestSharp;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace TestUtil {

    class Program {

        public static string Host { get; private set; }
        public static int Port { get; private set; }

        private static RestClient Client {
            get {
                return new RestClient($"http://{Host}:{Port}/api");
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
            var privateKey = WomPlatform.Web.Api.KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("../../testkeys/source1.pem").Private;
            var crypto = new WomPlatform.Web.Api.CryptoProvider();

            var request = new RestRequest("voucher/create", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Accept", "application/json");
            request.AddJsonBody(new WomPlatform.Web.Api.Models.CreatePayload {
                SourceId = 1,
                Nonce = Guid.NewGuid(),
                Payload = crypto.EncryptPayload(new WomPlatform.Web.Api.Models.CreatePayloadContent {
                    Id = Guid.NewGuid(),
                    SourceId = 1,
                    Vouchers = new WomPlatform.Web.Api.Models.VoucherRequestInfo[] { }
                }, privateKey)
            });

            var response = Client.Execute(request);
            Console.WriteLine("HTTP {0}, {1} bytes, {2}", response.StatusCode, response.ContentLength, response.ContentType);
            Console.WriteLine("Response: {0}", response.Content);
        }

    }

}
