using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Controllers {

    [Route("")]
    public class StaticController : Controller {

        protected MongoDatabase Mongo;
        protected ILogger<StaticController> Logger { get; }

        public StaticController(
            MongoDatabase mongo,
            ILogger<StaticController> logger
        ) {
            Mongo = mongo;
            Logger = logger;
        }

        [HttpGet("diary")]
        public IActionResult ShowDiaryLandingPage() {
            return View("Diary");
        }

        [HttpGet("pocket")]
        public IActionResult ShowPocketLandingPage() {
            return View("Pocket");
        }

        [HttpGet("pos")]
        public IActionResult ShowPosLandingPage() {
            return View("Pos");
        }

    }

}
