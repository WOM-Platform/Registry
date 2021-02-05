using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Provides access to a list of aims.
    /// </summary>
    [Route("v2/aims")]
    [OperationsTags("Aims")]
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

        /// <summary>
        /// Retrieves a list of all aims recognized by the WOM Platform.
        /// </summary>
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
