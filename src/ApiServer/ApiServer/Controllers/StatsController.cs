using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Connector;
using WomPlatform.Web.Api.OutputModels.Stats;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/stats")]
    [OperationsTags("Stats and info")]
    public class StatsController : BaseRegistryController {

        private readonly MongoDatabase _mongo;
        private readonly StatsService _statsService;

        public StatsController(
            MongoDatabase mongo,
            StatsService statsService,
            IServiceProvider serviceProvider,
            ILogger<AdminController> logger)
        : base(serviceProvider, logger) {
            _mongo = mongo;
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

            var totalGenerated = results.Sum(a => a.TotalCount);
            var totalAvailable = results.Sum(a => a.AvailableCount);

            return Ok(new VouchersGeneralStatsResponse {
                TotalVouchersGenerated = totalGenerated,
                TotalVouchersRedeemed = results.Sum(a => a.RedeemedCount),
                TotalVouchersAvailable = totalAvailable,
                TotalVouchersSpent = totalGenerated - totalAvailable,
                Aims = results.ToDictionary(
                    a => a.AimCode,
                    a => new VouchersGeneralStatsResponse.VouchersByAimStatsResponse {
                        Generated = a.TotalCount,
                        Redeemed = a.RedeemedCount,
                        Available = a.AvailableCount,
                        Spent = a.TotalCount - a.AvailableCount
                    }
                )
            });
        }

        /// <summary>
        /// Provides a count of vouchers produced by a given source.
        /// Request must be authorized by a user who is an administrator of the source.
        /// </summary>
        [HttpGet("vouchers/{sourceId}")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(VoucherSourceStatsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSourceVoucherStats(
            [FromRoute] ObjectId sourceId
        ) {
            var source = await _mongo.GetSourceById(sourceId);
            if(source == null) {
                return NotFound();
            }

            if(!User.GetUserId(out var loggedUserId) || !source.AdministratorUserIds.Contains(loggedUserId)) {
                return Forbid();
            }

            var result = await _statsService.GetVoucherCountBySource(sourceId);

            return Ok(new VoucherSourceStatsResponse {
                GenerationRequests = result?.GenerationRequests ?? 0,
                TotalVouchersGenerated = result?.TotalCount ?? 0,
                TotalVouchersRedeemed = result?.RedeemedCount ?? 0
            });
        }

    }

}
