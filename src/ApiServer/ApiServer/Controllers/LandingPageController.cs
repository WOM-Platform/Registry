using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.Controllers {

    [Route("")]
    public class LandingPageController : Controller {

        protected ILogger<LandingPageController> Logger { get; }

        public LandingPageController(
            ILogger<LandingPageController> logger
        ) {
            Logger = logger;
        }

        [HttpGet("payment/{otc}")]
        public IActionResult ShowPaymentLandingPage(
            [FromRoute] Guid otc
        ) {
            return View("Payment", new LandingPageViewModel {
                Otc = otc
            });
        }

        [HttpGet("vouchers/{otc}")]
        public IActionResult ShowGenerationLandingPage(
            [FromRoute] Guid otc
        ) {
            return View("Generation", new LandingPageViewModel {
                Otc = otc
            });
        }

    }

}
