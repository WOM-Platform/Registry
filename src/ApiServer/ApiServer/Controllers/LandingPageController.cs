using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.Controllers {

    [Route("")]
    public class LandingPageController : BaseRegistryController {

        public LandingPageController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<LandingPageController> logger)
        : base(configuration, crypto, keyManager, mongo, @operator, logger) {

        }

        [HttpGet("payment/{otc}")]
        public async Task<IActionResult> ShowPaymentLandingPage(
            [FromRoute] Guid otc
        ) {
            Logger.LogDebug("Showing landing page for payment request {0}", otc);

            var payment = await Mongo.GetPaymentRequestByOtc(otc);
            if(payment == null) {
                return View("NotFound");
            }
            else {
                return View("Payment", new LandingPageViewModel {
                    Otc = otc
                });
            }
        }

        [HttpGet("vouchers/{otc}")]
        public async Task<IActionResult> ShowGenerationLandingPage(
            [FromRoute] Guid otc
        ) {
            Logger.LogDebug("Showing landing page for voucher generation {0}", otc);

            var generation = await Mongo.GetGenerationRequestByOtc(otc);
            if(generation == null ||
                generation.PerformedAt.HasValue ||
                generation.Void ||
                generation.Attempts <= 0) {
                return View("NotFound");
            }
            else {
                return View("Generation", new LandingPageViewModel {
                    Otc = otc
                });
            }
        }

    }

}
