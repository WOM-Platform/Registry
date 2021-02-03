using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    [Authorize(Policy = Startup.UserLoginPolicy)]
    [Route("dashboard")]
    public class DashboardController : Controller {

        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ILogger<DashboardController> logger
        ) {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index() {
            return Ok();
        }

    }

}
