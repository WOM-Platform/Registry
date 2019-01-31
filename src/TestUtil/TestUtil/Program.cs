using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using RestSharp;
using System;
using System.IO;
using System.Linq;
using System.Text;
using TestUtil.Operations;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.Models;

namespace TestUtil {

    class Program {

        public static void Main(string[] args) {
            if(args.Length < 3) {
                Console.Error.WriteLine("Usage: TestUtil <host> <port> command");
                Environment.Exit(1);
            }

            try {
                BaseOperation op = CreateOperation(args[2]);
                op.Host = args[0];
                op.Port = int.Parse(args[1]);
                op.Crypto = new CryptoProvider(new ConsoleLogger<CryptoProvider>());

                op.Execute(args.Skip(3).ToArray());
            }
            catch(Exception ex) {
                Console.Error.WriteLine("Error: {0}", ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
            }

            Console.Read();
        }

        private static BaseOperation CreateOperation(string code) {
            switch (code) {
                case "vcreate":
                    return new VoucherCreationOperation();

                case "vredeem":
                    return new VoucherRedemptionOperation();

                default:
                    throw new ArgumentException("Unsupported operation", nameof(code));
            }
        }

/*
        private static void RedeemVouchers(string[] args) {
            if(args.Length < 1) {
                throw new ArgumentNullException("Requires voucher redemption ID");
            }
            if(!Guid.TryParse(args[0], out Guid redemptionId)) {
                throw new ArgumentException("Requires voucher redemption ID as GUID");
            }

            var publicKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("../../../testkeys/registry.pem").Public;
            var crypto = new CryptoProvider(new ConsoleLogger<CryptoProvider>());

            var request = new RestRequest("voucher/redeem", Method.POST) {
                RequestFormat = DataFormat.Json
            };
            request.AddHeader("Accept", "application/json");
            request.AddJsonBody(new VoucherRedeemPayload {
                Payload = crypto.Encrypt(new VoucherRedeemPayload.Content {
                    Otc = redemptionId
                }, publicKey)
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
        }*/

    }

}
