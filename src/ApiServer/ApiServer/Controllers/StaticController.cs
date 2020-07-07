using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.ViewModel;

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
        public async Task<IActionResult> ShowPosLandingPage() {
            var pos = await Mongo.GetPosWithPosition();

            return View("Pos", new PosMapViewModel {
                Pos = pos
            });
        }

    }

}
