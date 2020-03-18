using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/v1/aim")]
    public class AimController : BaseRegistryController {

        public AimController(
            IConfiguration configuration,
            MongoDatabase mongo,
            DatabaseOperator database,
            KeyManager keyManager,
            CryptoProvider crypto,
            ILogger<AimController> logger)
        : base(configuration, crypto, keyManager, mongo, database, logger) {
        }

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
