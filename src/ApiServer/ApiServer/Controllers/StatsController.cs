using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;

namespace WomPlatform.Web.Api.Controllers {

    [ApiController]
    [Route("v1/stats")]
    public class StatsController : BaseRegistryController {

        public StatsController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, mongo, @operator, logger) {
        }

        [HttpGet("vouchers")]
        public async Task<IActionResult> GetVoucherStats() {
            var data = await Mongo.GetVoucherStats();
            return Ok(data);
        }

    }

}
