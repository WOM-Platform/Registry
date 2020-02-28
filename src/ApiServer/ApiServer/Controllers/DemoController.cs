using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Connector.Models;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.Controllers {

    [Route("demo")]
    public class DemoController : Controller {

        private readonly IConfiguration _configuration;
        private readonly LinkGenerator _linkGenerator;
        private readonly DatabaseOperator _database;
        private readonly ILogger<DemoController> _logger;

        public DemoController(
            IConfiguration configuration,
            LinkGenerator linkGenerator,
            DatabaseOperator database,
            ILogger<DemoController> logger
        ) {
            _configuration = configuration;
            _linkGenerator = linkGenerator;
            _database = database;
            _logger = logger;
        }

        [HttpGet("redeem")]
        public IActionResult ShowPocketDemo() {
            return View("Pocket");
        }

        [HttpPost("redeem")]
        public async Task<IActionResult> RequestVoucher() {
            var confSection = _configuration.GetSection("Demo");
            var amount = Convert.ToInt32(confSection["AmountOfVouchers"]);
            var sourceId = Convert.ToInt32(confSection["SourceId"]);

            (var otc, var pwd) = await _database.CreateVoucherGeneration(new Connector.Models.VoucherCreatePayload.Content {
                Nonce = Guid.NewGuid().ToString("N"),
                SourceId = sourceId,
                Vouchers = new VoucherCreatePayload.VoucherInfo[] {
                    new VoucherCreatePayload.VoucherInfo {
                        Aim = "0",
                        Count = amount,
                        Timestamp = DateTime.UtcNow,
                        Latitude = 43.726,
                        Longitude = 12.636
                    }
                }
            }, isPreVerified: true);

            string redemptionUrl = _linkGenerator.GetUriByAction(
                nameof(LandingPageController.ShowGenerationLandingPage),
                "LandingPage",
                new {
                    otc = otc
                },
                "https",
                new HostString(Environment.GetEnvironmentVariable("SELF_HOST"))
            );

            return View("PocketShow", new DemoLinkViewModel {
                Otc = otc,
                OtcUrl = redemptionUrl,
                Password = pwd
            });
        }

        [HttpGet("pay")]
        public IActionResult ShowPosDemo() {
            return View("Pos");
        }

        [HttpPost("pay")]
        public async Task<IActionResult> RequestPayment(
            [FromForm] string demoDiscount,
            [FromForm] int demoAmount
        ) {
            var confSection = _configuration.GetSection("Demo");
            var amount = Convert.ToInt32(confSection["AmountOfVouchers"]);
            var posId = Convert.ToInt32(confSection["PosId"]);

            string completionUrl = _linkGenerator.GetUriByAction(
                nameof(DemoController.RequestPayment),
                "Demo",
                new {
                    item = demoDiscount,
                    amount = demoAmount
                },
                "https",
                new HostString(Environment.GetEnvironmentVariable("SELF_HOST"))
            );

            (var otc, var pwd) = await _database.CreatePaymentRequest(new Connector.Models.PaymentRegisterPayload.Content {
                Nonce = Guid.NewGuid().ToString("N"),
                PosId = posId,
                Amount = 1,
                PocketAckUrl = completionUrl,
                SimpleFilter = new SimpleFilter {
                    Aim = "0"
                }
            }, isPreVerified: true);

            string redemptionUrl = _linkGenerator.GetUriByAction(
                nameof(LandingPageController.ShowPaymentLandingPage),
                "LandingPage",
                new {
                    otc = otc
                },
                "https",
                new HostString(Environment.GetEnvironmentVariable("SELF_HOST"))
            );

            return View("PosShow", new DemoLinkViewModel {
                Otc = otc,
                OtcUrl = redemptionUrl,
                Password = pwd
            });
        }

        [HttpGet("pay-complete")]
        public IActionResult ShowDiscount(
            [FromQuery] string item,
            [FromQuery] int amount
        ) {
            ViewData["Item"] = item;
            ViewData["Amount"] = amount;

            return View("PosComplete");
        }

    }

}
