using System;
using System.Collections.Generic;
using System.Text;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.Models;

namespace TestUtil.Operations {

    public class VoucherRedemptionOperation : BaseOperation {

        public override void Execute(string[] args) {
            if (args.Length < 2) {
                throw new ArgumentNullException("Requires voucher redemption ID and Password");
            }
            if (!Guid.TryParse(args[0], out Guid redemptionId)) {
                throw new ArgumentException("Requires voucher redemption ID as GUID");
            }

            var rnd = new Random();
            var publicRegistryKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("registry.pem").Public;
            byte[] sessionKey = new byte[256 / 8];
            rnd.NextBytes(sessionKey);

            var request = CreateJsonRequest("voucher/redeem", new VoucherRedeemPayload {
                Payload = Crypto.Encrypt(new VoucherRedeemPayload.Content {
                    Otc = redemptionId,
                    Password = args[1],
                    SessionKey = sessionKey.ToBase64()
                }, publicRegistryKey)
            });

            var response = PerformRequest<VoucherRedeemResponse>(request);
            var responseContent = Crypto.Decrypt<VoucherRedeemResponse.Content>(response.Payload, sessionKey);
            Console.WriteLine("Vouchers redeemed");
            foreach (var v in responseContent.Vouchers) {
                Console.WriteLine("V #{0} aim {1}", v.Id, v.Aim);
                Console.WriteLine("  @ {2} in {0},{1}", v.Latitude, v.Longitude, v.Timestamp);
            }
        }

    }

}
