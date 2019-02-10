using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.Models;

namespace TestUtil.Operations {

    public class VoucherCreationOperation : BaseOperation {

        public override void Execute(string[] args) {
            var rnd = new Random();
            var password = rnd.GeneratePassword(4);

            var privateKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("source1.pem").Private;
            var publicRegistryKey = KeyManager.LoadKeyFromPem<AsymmetricCipherKeyPair>("registry.pem").Public;

            var now = DateTime.UtcNow;
            var voucherInfos = new List<VoucherCreatePayload.VoucherInfo>();
            for (int i = 0; i < 5; ++i) {
                voucherInfos.Add(new VoucherCreatePayload.VoucherInfo {
                    Aim = "1",
                    Latitude = rnd.NextBetween(5, 40),
                    Longitude = rnd.NextBetween(5, 50),
                    Timestamp = now
                });

                now = now.Subtract(TimeSpan.FromMinutes(30));
            }

            var nonce = Guid.NewGuid().ToString("N");
            var request = CreateJsonRequest("voucher/create", new VoucherCreatePayload {
                SourceId = 1,
                Nonce = nonce,
                Payload = Crypto.Encrypt(new VoucherCreatePayload.Content {
                    SourceId = 1,
                    Nonce = nonce,
                    Password = password,
                    Vouchers = voucherInfos.ToArray()
                }, publicRegistryKey)
            });

            var response = PerformRequest<VoucherCreateResponse>(request);
            var responseContent = Crypto.Decrypt<VoucherCreateResponse.Content>(response.Payload, privateKey);
            Console.WriteLine("Voucher generation requested");
            Console.WriteLine(JsonConvert.SerializeObject(responseContent, Formatting.Indented));
            Console.WriteLine();
            Console.WriteLine("{0:N} {1}", responseContent.Otc, password);
        }
    }

}
