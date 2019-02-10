using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.Models;

namespace ApiServer.Controllers {

    [Route("api/debug")]
    public class TestController : ControllerBase {

        public TestController(
            IConfiguration configuration,
            DataContext database,
            CryptoProvider cryptoProvider,
            IHostingEnvironment hosting,
            ILogger<TestController> logger
        ) {
            Configuration = configuration;
            Database = database;
            Crypto = cryptoProvider;
            Hosting = hosting;
            Logger = logger;
        }

        protected IConfiguration Configuration { get; }
        protected DataContext Database { get; }
        protected CryptoProvider Crypto { get; }
        protected IHostingEnvironment Hosting { get; }
        protected ILogger<TestController> Logger { get; }

        [HttpPost("create-vouchers/{count=10}")]
        public IActionResult CreateVouchers([FromRoute]int count) {
            if(!Hosting.IsDevelopment()) {
                return Unauthorized();
            }

            if(count <= 0 || count > 100) {
                throw new ArgumentOutOfRangeException(nameof(count), "Voucher count out of range");
            }

            var pin = Crypto.Generator.GeneratePassword(4);

            Logger.LogInformation("Creating {0} test vouchers", count);

            var testSource = Database.GetSourceById(1);
            var aims = Database.GetAims().ToList();

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

            var result = Database.CreateVoucherGeneration(new VoucherCreatePayload.Content {
                SourceId = testSource.Id,
                Nonce = Guid.NewGuid().ToString("N"),
                Password = pin,
                Vouchers = voucherInfos.ToArray()
            });

            Logger.LogDebug("New voucher generation request created with code {0}", result);

            Database.VerifyGenerationRequest(result);

            Logger.LogDebug("Voucher generation request verified");

            return Ok( new {
                OtcGen = UrlGenerator.GenerateRedeemUrl(result),
                Pin = pin
            });
        }

    }

}
