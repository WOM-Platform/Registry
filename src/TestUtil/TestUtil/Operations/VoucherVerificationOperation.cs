using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.Models;

namespace TestUtil.Operations {

    public class VoucherVerificationOperation : BaseOperation {

        public override void Execute(string[] args) {
            if (args.Length < 1) {
                throw new ArgumentNullException("Requires voucher redemption OTC");
            }
            if (!Guid.TryParse(args[0], out Guid redemptionId)) {
                throw new ArgumentException("Requires voucher redemption OTC as GUID");
            }

            var publicRegistryKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("registry.pem").Public;

            var nonce = Guid.NewGuid().ToString("N");
            var request = CreateJsonRequest("voucher/verify", new VoucherVerifyPayload {
                Payload = Crypto.Encrypt(new VoucherVerifyPayload.Content {
                    Otc = redemptionId
                }, publicRegistryKey)
            });

            var response = PerformRequest<VoucherCreateResponse>(request);
            Console.WriteLine("Voucher generation verified");
        }
    }

}
