using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Provides access to single aims.
    /// </summary>
    [Route("v1/aim")]
    [OperationsTags("Aims")]
    public class AimController : BaseRegistryController {

        private readonly MongoDatabase _mongo;

        public AimController(
            MongoDatabase mongo,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, logger) {
            _mongo = mongo;
        }

        public record AimShowOutput(
            string Code,
            Dictionary<string, string> Titles
        );

        /// <summary>
        /// Retrieve information about an aim.
        /// </summary>
        /// <param name="code">Aim code (ex. 'H').</param>
        [HttpGet("{*code}")]
        [ChangeLog("aim-list")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AimShowOutput), StatusCodes.Status200OK)]
        public async Task<IActionResult> Show(string code) {
            var cleanCode = code.Replace("/", string.Empty);

            var aim = await _mongo.GetAimByCode(cleanCode);
            if(aim == null) {
                return NotFound();
            }

            return Ok(new AimShowOutput(aim.Code, aim.Titles));
        }

    }

}
