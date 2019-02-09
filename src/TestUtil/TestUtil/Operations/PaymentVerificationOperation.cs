using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.Models;

namespace TestUtil.Operations {

    public class PaymentVerificationOperation : BaseOperation {

        public override void Execute(string[] args) {
            if (args.Length < 1) {
                throw new ArgumentNullException("Requires voucher redemption OTC");
            }
            if (!Guid.TryParse(args[0], out Guid redemptionId)) {
                throw new ArgumentException("Requires voucher redemption OTC as GUID");
            }

            var publicRegistryKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("registry.pem").Public;

            var nonce = Guid.NewGuid().ToString("N");
            var request = CreateJsonRequest("payment/verify", new PaymentVerifyPayload {
                Payload = Crypto.Encrypt(new PaymentVerifyPayload.Content {
                    Otc = redemptionId
                }, publicRegistryKey)
            });

            var response = PerformRequest<VoucherCreateResponse>(request);
            Console.WriteLine("Payment verified");
        }
    }

}
