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

        public AimController(
            IServiceProvider serviceProvider,
            ILogger<AimController> logger)
        : base(serviceProvider, logger) {
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

            var aim = await AimService.GetAimByCode(cleanCode);
            if(aim == null) {
                return NotFound();
            }

            return Ok(new AimShowOutput(aim.Code, aim.Titles));
        }

    }

}
