using Newtonsoft.Json;
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

                case "vredeem":
                    RedeemVouchers(args.Skip(3).ToArray());
                    break;

                default:
                    Console.Error.WriteLine("Unknown command");
                    Environment.Exit(1);
                    break;
            }
        }

        private static void CreateVouchers(string[] args) {
            var privateKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("../../../testkeys/source1.pem").Private;
            var crypto = new CryptoProvider(new ConsoleLogger<CryptoProvider>());

            var nonce = Guid.NewGuid();
            /*
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
            Console.WriteLine("OTC: {0}", decryptedOtc);*/
        }

        private static void RedeemVouchers(string[] args) {
            if(args.Length < 1) {
                throw new ArgumentNullException("Requires voucher redemption ID");
            }
            if(!Guid.TryParse(args[0], out Guid redemptionId)) {
                throw new ArgumentException("Requires voucher redemption ID as GUID");
            }

            var publicKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("../../../testkeys/registry.pem").Public;
            var crypto = new CryptoProvider(new ConsoleLogger<CryptoProvider>());

            var request = new RestRequest("voucher/redeem/" + redemptionId.ToString("N"), Method.POST) {
                RequestFormat = DataFormat.Json
            };
            request.AddHeader("Accept", "application/json");
            request.AddJsonBody(new VoucherRedeemPayload {
                Nonce = Guid.NewGuid()
            });

            var response = Client.Execute(request);
            Console.WriteLine("HTTP {0}, {1} bytes, {2}", response.StatusCode, response.ContentLength, response.ContentType);
            var data = JsonConvert.DeserializeObject<VoucherRedeemResponse>(response.Content);
            var content = crypto.Verify<VoucherRedeemResponse.Content>(data.Payload, publicKey);
            Console.WriteLine("Response contains {0} vouchers:", content.Vouchers.Length);
            foreach(var v in content.Vouchers) {
                Console.WriteLine();
                Console.WriteLine("V #{0} from {1}", v.Id, v.Source);
                Console.WriteLine("  @ {2} in {0},{1}", v.Latitude, v.Longitude, v.Timestamp);
            }
            Console.WriteLine("===");
        }

    }

}
