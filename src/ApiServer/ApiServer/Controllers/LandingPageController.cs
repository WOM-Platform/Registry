using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Backwards compatibility controller that redirects to new voucher/payment landing pages.
    /// </summary>
    [Route("")]
    [OperationsTags("Landing pages")]
    public class LandingPageController : BaseRegistryController {

        public LandingPageController(
            IServiceProvider serviceProvider,
            ILogger<AdminController> logger)
        : base(serviceProvider, logger) {

        }

        [HttpGet("payment/{otc}")]
        [ProducesResponseType(typeof(void), StatusCodes.Status301MovedPermanently)]
        public IActionResult ShowPaymentLandingPage(
            [FromRoute] Guid otc
        ) {
            return RedirectPermanent($"https://{SelfLinkDomain}/payment/{otc:D}");
        }

        [HttpGet("vouchers/{otc}")]
        [ProducesResponseType(typeof(void), StatusCodes.Status301MovedPermanently)]
        public IActionResult ShowGenerationLandingPage(
            [FromRoute] Guid otc
        ) {
            return RedirectPermanent($"https://{SelfLinkDomain}/vouchers/{otc:D}");
        }

    }

}
