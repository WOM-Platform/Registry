using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;

namespace WomPlatform.Web.Api.Controllers {

    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AimsController : BaseRegistryController {

        public AimsController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, mongo, @operator, logger) {
        }

        // GET /api/v1/aims
        [Produces("application/json")]
        [HttpGet]
        [HttpHead]
        [ChangeLog("aim-list")]
        [ApiVersion("1")]
        public async Task<IActionResult> List() {
            var aims = await Mongo.GetAims();
            return Ok(from a in aims
                      select new {
                          a.Code,
                          a.Titles,
                          a.Order
                      });
        }

        // GET /api/v2/aims
        [Produces("application/json")]
        [HttpGet]
        [HttpHead]
        [ChangeLog("aim-list")]
        [ApiVersion("2")]
        public async Task<IActionResult> ListV2() {
            var aims = await Mongo.GetAims();
            var aimList = from a in aims
                          select new {
                              a.Code,
                              a.Titles,
                              a.Order
                          };
            return Ok(new {
                Aims = aimList
            });
        }

    }

}
