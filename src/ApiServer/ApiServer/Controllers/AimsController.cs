using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Provides access to a list of aims.
    /// </summary>
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/aims")]
    [OperationsTags("Aims")]
    public class AimsController : BaseRegistryController {

        private readonly MongoDatabase _mongo;

        public AimsController(
            MongoDatabase mongo,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, logger) {
            _mongo = mongo;
        }

        /// <summary>
        /// Retrieves a list of all aims recognized by the WOM Platform.
        /// </summary>
        [Produces("application/json")]
        [HttpGet]
        [HttpHead]
        [ChangeLog("aim-list")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> List() {
            var aims = await _mongo.GetAims();
            return Ok(from a in aims
                      select new {
                          a.Code,
                          a.Titles,
                          a.Order
                      });
        }

    }

}
