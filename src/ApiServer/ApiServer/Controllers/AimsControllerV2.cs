using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v2/aims")]
    public class AimsControllerV2 : BaseRegistryController {

        public AimsControllerV2(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<AimsControllerV2> logger)
        : base(configuration, crypto, keyManager, mongo, @operator, logger) {
        }

        // GET /api/v2/aims
        [Produces("application/json")]
        [HttpGet]
        [HttpHead]
        [ChangeLog("aim-list")]
        /// <summary>
        /// Get list of all aims.
        /// </summary>
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
