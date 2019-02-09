using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.Models;

namespace TestUtil.Operations {

    public class PaymentCreationOperation : BaseOperation {

        public override void Execute(string[] args) {
            var rnd = new Random();
            var password = rnd.GeneratePassword(4);

            var privateKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("pos1.pem").Private;
            var publicRegistryKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("registry.pem").Public;

            var nonce = Guid.NewGuid().ToString("N");
            var request = CreateJsonRequest("payment/register", new PaymentRegisterPayload {
                PosId = 1,
                Nonce = nonce,
                Payload = Crypto.Encrypt(new PaymentRegisterPayload.Content {
                    PosId = 1,
                    Nonce = nonce,
                    Password = password,
                    Amount = 10,
                    PosAckUrl = string.Format("https://example.org/pos/test/{0:N}", nonce),
                    PocketAckUrl = string.Format("app://wom/{0:N}", nonce),
                    SimpleFilter = new SimpleFilter {
                        Aim = UrlGenerator.GenerateAimUrl("1"),
                        MaxAge = 14
                    }
                }, publicRegistryKey)
            });

            var response = PerformRequest<PaymentRegisterResponse>(request);
            var responseContent = Crypto.Decrypt<PaymentRegisterResponse.Content>(response.Payload, privateKey);
            Console.WriteLine("Payment generated");
            Console.WriteLine(JsonConvert.SerializeObject(responseContent, Formatting.Indented));
            Console.WriteLine();
            Console.WriteLine("OTCgen: {0:N}", responseContent.Otc);
            Console.WriteLine("Password: {0}", password);
        }
    }

}
