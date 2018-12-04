using System;
using System.Collections.Generic;
using System.Text;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api.Models;

namespace TestUtil.Operations {

    public class VoucherRedemptionOperation : BaseOperation {

        public override void Execute(string[] args) {
            if (args.Length < 1) {
                throw new ArgumentNullException("Requires voucher redemption ID");
            }
            if (!Guid.TryParse(args[0], out Guid redemptionId)) {
                throw new ArgumentException("Requires voucher redemption ID as GUID");
            }

            var publicRegistryKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("registry.pem").Public;

            var request = CreateJsonRequest("voucher/redeem", new VoucherRedeemPayload {
                Payload = Crypto.Encrypt(new VoucherRedeemPayload.Content {
                    Otc = redemptionId
                }, publicRegistryKey)
            });

            var response = PerformRequest<VoucherRedeemResponse>(request);
            var responseContent = Crypto.Verify<VoucherRedeemResponse.Content>(response.Payload, publicRegistryKey);
            Console.WriteLine("Vouchers redeemed");
            foreach (var v in responseContent.Vouchers) {
                Console.WriteLine("V #{0} from {1}", v.Id, v.Source);
                Console.WriteLine("  @ {2} in {0},{1}", v.Latitude, v.Longitude, v.Timestamp);
            }
        }

    }

}
