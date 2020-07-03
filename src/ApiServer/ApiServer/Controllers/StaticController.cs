using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    [Route("")]
    public class StaticController : Controller {

        protected ILogger<StaticController> Logger { get; }

        public StaticController(
            ILogger<StaticController> logger
        ) {
            Logger = logger;
        }

        [HttpGet("diary")]
        public IActionResult ShowDiaryLandingPage() {
            return View("Diary");
        }

    }

}
