using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.Models;

namespace ApiServer.Controllers {

    [Route("api/debug")]
    public class TestController : ControllerBase {

        protected readonly Random _rnd = new Random();

        public TestController(
            IConfiguration configuration,
            DatabaseManager database,
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
        protected DatabaseManager Database { get; }
        protected CryptoProvider Crypto { get; }
        protected IHostingEnvironment Hosting { get; }
        protected ILogger<TestController> Logger { get; }

        [HttpPost("create-vouchers/{count=10}")]
        public IActionResult CreateVouchers([FromRoute]int count) {
            if(!Hosting.IsDevelopment()) {
                return this.Unauthorized();
            }

            if(count <= 0 || count > 100) {
                throw new ArgumentOutOfRangeException(nameof(count), "Voucher count out of range");
            }

            Logger.LogInformation("Creating {0} test vouchers", count);

            var testSource = Database.Connection.GetSourceById(1);

            var now = DateTime.UtcNow;
            var voucherInfos = new List<VoucherCreatePayload.VoucherInfo>();
            for(int i = 0; i < count; ++i) {
                voucherInfos.Add(new VoucherCreatePayload.VoucherInfo {
                    Latitude = _rnd.NextBetween(5, 40),
                    Longitude = _rnd.NextBetween(5, 50),
                    Timestamp = now
                });

                now = now.Subtract(TimeSpan.FromMinutes(5));
            }

            var result = Database.Connection.CreateVoucherGeneration(new VoucherCreatePayload.Content {
                SourceId = testSource.Id,
                Vouchers = voucherInfos.ToArray()
            });

            Logger.LogTrace("New voucher generation request created with code {0}", result);

            return Content(result.ToString("N"));
        }

    }

}
