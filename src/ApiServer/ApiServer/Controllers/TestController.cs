using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WomPlatform.Connector;
using WomPlatform.Connector.Models;

namespace WomPlatform.Web.Api.Controllers {

    [Produces("application/json")]
    [Route("api/debug")]
    public class TestController : BaseRegistryController {

        public TestController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            DatabaseOperator database,
            IWebHostEnvironment hosting,
            ILogger<TestController> logger
        ) : base(configuration, crypto, keyManager, database, logger) {
            Hosting = hosting;
        }

        protected IWebHostEnvironment Hosting { get; }

        [HttpPost("create-vouchers/{count=10}")]
        public async Task<ActionResult> CreateVouchers(
            [FromRoute] int count
        ) {
            if(!Hosting.IsDevelopment()) {
                return Unauthorized();
            }

            if(count <= 0 || count > 100) {
                throw new ArgumentOutOfRangeException(nameof(count), "Voucher count out of range");
            }

            Logger.LogInformation("Creating {0} test vouchers", count);

            var testSource = await Database.GetSourceById(1);
            var aims = Database.GetFlatAims().ToList();

            Logger.LogTrace("Test source: {0}, aims: {1}", testSource.Name, string.Join(", ", from a in aims select a.Code));

            var now = DateTime.UtcNow;
            var voucherInfos = new List<VoucherCreatePayload.VoucherInfo>();
            for(int i = 0; i < count; ++i) {
                voucherInfos.Add(new VoucherCreatePayload.VoucherInfo {
                    Aim = Crypto.Generator.Pick(aims).Code,
                    Latitude = Crypto.Generator.NextBetween(5, 40),
                    Longitude = Crypto.Generator.NextBetween(5, 50),
                    Timestamp = now
                });

                now = now.Subtract(TimeSpan.FromMinutes(5));
            }

            (var result, var password) = await Database.CreateVoucherGeneration(new VoucherCreatePayload.Content {
                SourceId = testSource.Id.ToId(),
                Nonce = Guid.NewGuid().ToString("N"),
                Vouchers = voucherInfos.ToArray()
            });

            Logger.LogDebug("New voucher generation request created with code {0}", result);

            Database.VerifyGenerationRequest(result);

            Logger.LogDebug("Voucher generation request verified");

            return Ok(new {
                OtcGen = UrlGenerator.GenerateRedeemUrl(result),
                Pin = password
            });
        }

        [HttpPost("create-payment")]
        public ActionResult CreatePayment(
            string ackUrl,
            int amount = 10,
            [FromBody] SimpleFilter filter = null
        ) {
            if (!Hosting.IsDevelopment()) {
                return Unauthorized();
            }

            if(amount <= 0) {
                throw new ArgumentOutOfRangeException("Amount cannot be zero or negative", nameof(amount));
            }
            if(string.IsNullOrWhiteSpace(ackUrl)) {
                throw new ArgumentException("Acknowledgment URL cannot be void", nameof(ackUrl));
            }

            Logger.LogInformation("Creating payment for {0} vouchers with filter: {1}", amount, JsonConvert.SerializeObject(filter));

            var testPos = Database.GetPosById(1);

            (var otcPay, var pin) = Database.CreatePaymentRequest(new PaymentRegisterPayload.Content {
                Amount = amount,
                Nonce = Guid.NewGuid().ToString("N"),
                SimpleFilter = filter,
                PocketAckUrl = ackUrl,
                PosId = testPos.Id.ToId()
            });

            Logger.LogDebug("New payment request created with code {0}", otcPay);

            Database.VerifyPaymentRequest(otcPay);

            Logger.LogDebug("Payment request verified");

            return Ok(new {
                OtcPay = UrlGenerator.GeneratePaymentUrl(otcPay),
                Pin = pin
            });
        }

    }

}
