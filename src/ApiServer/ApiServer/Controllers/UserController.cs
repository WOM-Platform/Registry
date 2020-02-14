using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    [Route("user/{action:slugify}")]
    public class UserController : Controller {

        private readonly ILogger<UserController> _logger;

        public UserController(
            ILogger<UserController> logger
        ) {
            _logger = logger;
        }

        public IActionResult Login() {
            return Content("Login");
        }

        [HttpPost]
        public IActionResult PerformLogin(
        ) {
            return Content("Perform login");
        }

        public IActionResult Register() {
            return Content("Register");
        }

        public IActionResult Logout() {
            return Content("Logout");
        }

    }

}
