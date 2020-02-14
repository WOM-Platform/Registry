using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    [Authorize(Policy = Startup.UserLoginPolicy)]
    [Route("dashboard/[action]")]
    public class DashboardController : Controller {

        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ILogger<DashboardController> logger
        ) {
            _logger = logger;
        }

        public IActionResult Index() {
            return Content("Your dashboard here, lovely!");
        }

    }

}
