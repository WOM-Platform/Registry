using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Backwards compatibility controller that redirects to new voucher/payment landing pages.
    /// </summary>
    [Route("")]
    [OperationsTags("Landing pages")]
    public class LandingPageController : BaseRegistryController {

        public LandingPageController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<LandingPageController> logger)
        : base(configuration, crypto, keyManager, logger) {

        }

        [HttpGet("payment/{otc}")]
        public IActionResult ShowPaymentLandingPage(
            [FromRoute] Guid otc
        ) {
            return RedirectPermanent($"https://{SelfLinkDomain}/payment/{otc:D}");
        }

        [HttpGet("vouchers/{otc}")]
        public IActionResult ShowGenerationLandingPage(
            [FromRoute] Guid otc
        ) {
            return RedirectPermanent($"https://{SelfLinkDomain}/vouchers/{otc:D}");
        }

    }

}
