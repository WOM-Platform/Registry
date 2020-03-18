using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/v1/aims")]
    public class AimsController : BaseRegistryController {

        public AimsController(
            IConfiguration configuration,
            MongoDatabase mongo,
            DatabaseOperator database,
            KeyManager keyManager,
            CryptoProvider crypto,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, mongo, database, logger) {
        }

        [Produces("application/json")]
        [HttpGet]
        [HttpHead]
        [ChangeLog("aim-list")]
        public async Task<IActionResult> List() {
            var aims = await Mongo.GetAims();
            return Ok(aims);
        }

    }

}
