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

                case "vverify":
                    return new VoucherVerificationOperation();

                case "vredeem":
                    return new VoucherRedemptionOperation();

                case "pregister":
                    return new PaymentCreationOperation();

                case "pverify":
                    return new PaymentVerificationOperation();

                case "pinfo":
                    return new PaymentInformationOperation();

                default:
                    throw new ArgumentException("Unsupported operation", nameof(code));
            }
        }

    }

}
