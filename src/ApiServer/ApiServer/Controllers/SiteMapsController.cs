using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.Controllers {

    [Route("maps")]
    public class SiteMapsController : Controller {

        protected MongoDatabase Mongo;
        protected ILogger<SiteMapsController> Logger { get; }

        public SiteMapsController(
            MongoDatabase mongo,
            ILogger<SiteMapsController> logger
        ) {
            Mongo = mongo;
            Logger = logger;
        }

        [HttpGet("pos")]
        public async Task<IActionResult> ShowPosMap() {
            var pos = await Mongo.GetPosWithPosition();

            return View("Pos", new PosMapViewModel {
                Pos = pos
            });
        }

    }

}
