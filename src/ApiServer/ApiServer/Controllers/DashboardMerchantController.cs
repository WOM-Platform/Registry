using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    [Authorize(Policy = Startup.UserLoginPolicy)]
    [Route("dashboard/merchant")]
    public class DashboardMerchantController : Controller {

        private readonly ILogger<DashboardMerchantController> _logger;

        public DashboardMerchantController(
            ILogger<DashboardMerchantController> logger
        ) {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index() {
            return View("WaitForApproval");
        }

    }

}
