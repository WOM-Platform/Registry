using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;

namespace WomPlatform.Web.Api.Controllers {

    [ApiVersion("1")]
    [Route("v{version:apiVersion}/stats")]
    [OperationsTags("Stats and info")]
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

        /// <summary>
        /// Provides a count of all existing vouchers.
        /// </summary>
        [HttpGet("vouchers")]
        public async Task<IActionResult> GetVoucherStats() {
            var data = await Mongo.GetVoucherStats();
            return Ok(data);
        }

    }

}
