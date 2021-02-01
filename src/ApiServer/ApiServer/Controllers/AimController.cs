using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;

namespace WomPlatform.Web.Api.Controllers {

    [ApiController]
    [Route("api/v{version:apiVersion}/aim")]
    public class AimController : BaseRegistryController {

        public AimController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, mongo, @operator, logger) {
        }

        // GET /api/v*/aim/code
        [HttpGet("{*code}")]
        [ChangeLog("aim-list")]
        public async Task<IActionResult> Show(string code) {
            var cleanCode = code.Replace("/", string.Empty);

            var aim = await Mongo.GetAimByCode(cleanCode);
            if(aim == null) {
                return NotFound();
            }

            return Ok(new {
                aim.Code,
                aim.Titles
            });
        }

    }

}
