using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.Models;

namespace TestUtil.Operations {

    public class PaymentInformationOperation : BaseOperation {

        public override void Execute(string[] args) {
            if (args.Length < 2) {
                throw new ArgumentNullException("Requires payment OTC and Password");
            }
            if (!Guid.TryParse(args[0], out Guid redemptionId)) {
                throw new ArgumentException("Requires payment OTC as GUID");
            }

            var rnd = new Random();
            var publicRegistryKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("registry.pem").Public;
            byte[] sessionKey = new byte[256 / 8];
            rnd.NextBytes(sessionKey);

            var request = CreateJsonRequest("payment/info", new PaymentInfoPayload {
                Payload = Crypto.Encrypt(new PaymentInfoPayload.Content {
                    Otc = redemptionId,
                    Password = args[1],
                    SessionKey = sessionKey.ToBase64()
                }, publicRegistryKey)
            });

            var response = PerformRequest<PaymentInfoResponse>(request);
            var responseContent = Crypto.Decrypt<PaymentInfoResponse.Content>(response.Payload, sessionKey);
            Console.WriteLine("Payment info retrieved");
            Console.WriteLine(JsonConvert.SerializeObject(responseContent, Formatting.Indented));
        }
    }

}
