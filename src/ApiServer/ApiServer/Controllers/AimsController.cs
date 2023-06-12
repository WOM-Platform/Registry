using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Provides access to a list of aims.
    /// </summary>
    [Obsolete]
    [Route("v1/aims")]
    [OperationsTags("Aims")]
    public class AimsController : BaseRegistryController {

        private readonly AimService _aimService;

        public AimsController(
            AimService aimService,
            IServiceProvider serviceProvider,
            ILogger<AimsController> logger)
        : base(serviceProvider, logger) {
            _aimService = aimService;
        }

        public record AimListEntryOutput(
            string Code,
            Dictionary<string, string> Titles,
            int Order
        );

        /// <summary>
        /// Retrieves a list of all aims recognized by the WOM Platform.
        /// </summary>
        [HttpGet]
        [HttpHead]
        [ChangeLog("aim-list")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AimListEntryOutput[]), StatusCodes.Status200OK)]
        public async Task<IActionResult> List() {
            var aims = await _aimService.GetAllAims();
            return Ok(from a in aims
                      select new AimListEntryOutput(
                          a.Code,
                          a.Titles,
                          a.Order
                      ));
        }

    }

}
