using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Provides access to single aims.
    /// </summary>
    [Obsolete]
    [Route("v1/aim")]
    [OperationsTags("Aims")]
    public class AimController : BaseRegistryController {

        private readonly MongoDatabase _mongo;
        private readonly AimService _aimService;

        public AimController(
            MongoDatabase mongo,
            AimService aimService,
            IServiceProvider serviceProvider,
            ILogger<AdminController> logger)
        : base(serviceProvider, logger) {
            _mongo = mongo;
            _aimService = aimService;
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
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(AimShowOutput), StatusCodes.Status200OK)]
        public async Task<IActionResult> Show(string code) {
            var cleanCode = code.Replace("/", string.Empty);

            var aim = await _aimService.GetAimByCode(code);
            if(aim == null) {
                return NotFound();
            }

            return Ok(new AimShowOutput(aim.Code, aim.Titles));
        }

    }

}
