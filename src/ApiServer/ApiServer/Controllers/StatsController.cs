using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.DTO;
using WomPlatform.Web.Api.OutputModels.Stats;
using WomPlatform.Web.Api.Service;
using WomPlatform.Web.Api.Utilities;

namespace WomPlatform.Web.Api.Controllers;

[Route("v1/stats")]
[OperationsTags("Stats and info")]
[RequireHttpsInProd]
public class StatsController : BaseRegistryController {
    public StatsController(
        IServiceProvider serviceProvider,
        ILogger<StatsController> logger)
        : base(serviceProvider, logger) {
    }

    /// <summary>
    ///     Provides a count of all existing vouchers.
    /// </summary>
    [HttpGet("vouchers")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(VouchersGeneralStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVoucherStats() {
        var results = await StatsService.GetVoucherCountByAim();

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
    ///     Provides a count of vouchers produced by a given source.
    ///     Request must be authorized by a user who is an administrator of the source.
    /// </summary>
    [HttpGet("vouchers/{sourceId}")]
    [Authorize]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(VoucherSourceStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSourceVoucherStats(
        [FromRoute] ObjectId sourceId
    ) {
        var source = await SourceService.GetSourceById(sourceId);
        if(source == null) {
            return NotFound();
        }

        if(!User.GetUserId(out var loggedUserId) || !source.AdministratorUserIds.Contains(loggedUserId)) {
            return Forbid();
        }

        var result = await StatsService.GetVoucherCountBySource(sourceId);

        return Ok(new VoucherSourceStatsResponse {
            GenerationRequests = result?.GenerationRequests ?? 0,
            TotalVouchersGenerated = result?.TotalCount ?? 0,
        });
    }

        // API to send back the data for generation and redeemed vouchers
        [HttpPost("vouchers/generated-redeemed-statistics")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchVouchersGeneratedAndRedeemedStats([FromBody] StatisticsRequestDto request) {
            try {
                ObjectId[] sourceObjectIds = request.SourceId?.Select(id => new ObjectId(id)).ToArray() ?? Array.Empty<ObjectId>();
                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerSource(sourceObjectIds);

                (DateTime? parsedStartDate, DateTime? parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                // Call services to retrieve stats
                VoucherGenerationRedemptionStatsResponse response = await GenerationService.FetchTotalVouchersGeneratedAndRedeemedStats(parsedStartDate, parsedEndDate, sourceObjectIds, request.AimListFilter, request.Latitude, request.Longitude, request.Radius);

                return Ok(response);
            }
            catch(ServiceProblemException e) {
                return StatusCode(e.HttpStatus, e.Message);
            }
        }

        // API to send back the data for consumed vouchers
        [HttpPost("vouchers/consumed-statistics")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchVouchersConsumedStats([FromBody] StatisticsRequestDto request) {
            try {
                ObjectId[] merchantObjectIds = request.MerchantIds?.Select(id => new ObjectId(id)).ToArray() ?? Array.Empty<ObjectId>();

                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerMerchant(merchantObjectIds);

                (DateTime? parsedStartDate, DateTime? parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                // Fetch the total amount of consumed vouchers
                VoucherConsumptionStatsResponse consumedVouchers = await PaymentService.FetchTotalVouchersConsumedStats(parsedStartDate, parsedEndDate, merchantObjectIds);

                // Return the JSON response
                return Ok(consumedVouchers);
            }
            catch(ServiceProblemException ex) {
                return StatusCode(ex.HttpStatus, ex.Message);
            }
        }

        [HttpGet("offers/active")]
        public async Task<ActionResult<List<Offer>>> GetActiveOffers() {
            try {
                List<Offer> offers = await OfferService.GetActiveOffers();
                if(offers == null || offers.Count == 0) {
                    return NotFound("No active offers found.");
                }

                return Ok(offers);
            }
            catch(Exception ex) {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        ///     Get the total number of unused vouchers by position
        /// </summary>
        [HttpPost("voucher/available")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchVouchersAvailable(
            [FromBody] StatisticsRequestDto request
        ) {
            ObjectId[] merchantObjectIds = request.MerchantIds?.Select(id => new ObjectId(id)).ToArray() ?? Array.Empty<ObjectId>();

            // Think how to make a control on this api
            await IsUserAdminOrOwnerMerchant(merchantObjectIds);

            // Fetch the number of unused vouchers
            int numberUnusedVouchers = await GenerationService.FetchVouchersAvailable(request.Latitude, request.Longitude, request.Radius);

            // Return consumed vouchers divided for period
            return Ok(numberUnusedVouchers);
        }

        [HttpPost("download/csv")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadCsv([FromBody] StatisticsRequestDto request) {
            await VerifyUserIsAdmin(); // check if user is an admin

            ObjectId[] merchantObjectIds = request.MerchantIds?.Select(id => new ObjectId(id)).ToArray() ?? Array.Empty<ObjectId>();
            ObjectId[] sourceObjectIds = request.SourceId?.Select(id => new ObjectId(id)).ToArray() ?? Array.Empty<ObjectId>();

            // if dates present check dates are valid and in case parse them
            var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

            // create general API to call them and save the data
            var genRedResponse = await GenerationService.FetchTotalVouchersGeneratedAndRedeemedStats(parsedStartDate, parsedEndDate, sourceObjectIds, request.AimListFilter, request.Latitude, request.Longitude, request.Radius, true);
            var consumedResponse = await PaymentService.FetchTotalVouchersConsumedStats(parsedStartDate, parsedEndDate, merchantObjectIds, true);
            var availableResponse = await GenerationService.FetchVouchersAvailable(request.Latitude, request.Longitude, request.Radius);

            var filters = new FiltersDTO {
                StartDate = parsedStartDate,
                EndDate = parsedEndDate,
                SourceIds = sourceObjectIds,
                MerchantIds = merchantObjectIds,
                AimFilter = request.AimListFilter
            };

            var records = CsvFileHelper.GenerateCsvContent(genRedResponse, consumedResponse, availableResponse, filters);

            return File(records, "text/csv", $"{DateTime.Now:yyyy-M-d dddd}_stats.csv");
        }

}
