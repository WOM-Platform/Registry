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

        protected readonly IConfiguration _configuration;
        protected readonly DatabaseManager _database;
        protected readonly CryptoProvider _crypto;
        protected readonly IHostingEnvironment _hosting;
        protected readonly ILogger<TestController> _logger;

        protected readonly Random _rnd = new Random();

        public TestController(IConfiguration configuration, DatabaseManager database, CryptoProvider cryptoProvider,
            IHostingEnvironment hosting, ILogger<TestController> logger) {
            this._configuration = configuration;
            this._database = database;
            this._crypto = cryptoProvider;
            this._hosting = hosting;
            this._logger = logger;
        }

        [HttpPost("create-vouchers/{count=10}")]
        public IActionResult CreateVouchers([FromRoute]int count) {
            if(!this._hosting.IsDevelopment()) {
                return this.Unauthorized();
            }

            if(count <= 0 || count > 100) {
                throw new ArgumentOutOfRangeException(nameof(count), "Voucher count out of range");
            }

            this._logger.LogInformation("Creating {0} test vouchers", count);

            var testSource = this._database.Connection.GetSourceById(1);

            var now = DateTime.UtcNow;
            var voucherInfos = new List<VoucherCreatePayload.VoucherInfo>();
            for(int i = 0; i < count; ++i) {
                voucherInfos.Add(new VoucherCreatePayload.VoucherInfo {
                    Latitude = this._rnd.NextBetween(5, 40),
                    Longitude = this._rnd.NextBetween(5, 50),
                    Timestamp = now
                });

                now = now.Subtract(TimeSpan.FromMinutes(5));
            }

            var result = this._database.Connection.CreateVoucherGeneration(new VoucherCreatePayload.Content {
                SourceId = testSource.Id,
                Vouchers = voucherInfos.ToArray()
            });

            this._logger.LogTrace("New voucher generation request created with code {0}", result);

            return this.Content(result.ToString("N"));
        }

    }

}
