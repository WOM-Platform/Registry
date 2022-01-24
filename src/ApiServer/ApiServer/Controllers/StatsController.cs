using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.OutputModels.Stats;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [ApiVersion("1")]
    [Route("v{version:apiVersion}/stats")]
    [OperationsTags("Stats and info")]
    public class StatsController : BaseRegistryController {

        private readonly StatsService _statsService;

        public StatsController(
            StatsService statsService,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, logger) {
            _statsService = statsService;
        }

        /// <summary>
        /// Provides a count of all existing vouchers.
        /// </summary>
        [HttpGet("vouchers")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(VouchersGeneralStatsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVoucherStats() {
            var results = await _statsService.GetVoucherCountByAim();

            return Ok(new VouchersGeneralStatsResponse {
                TotalVouchersGenerated = results.Sum(a => a.TotalCount),
                TotalVouchersAvailable = results.Sum(a => a.AvailableCount),
                TotalVouchersClaimed = 0, // TODO
                Aims = results.ToDictionary(
                    a => a.AimCode,
                    a => new VouchersGeneralStatsResponse.VouchersByAimStatsResponse {
                        Generated = a.TotalCount,
                        Available = a.AvailableCount
                    }
                )
            });
        }

    }

}
