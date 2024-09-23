using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.DTO;
using WomPlatform.Web.Api.OutputModels.Stats;
using WomPlatform.Web.Api.Utilities;

namespace WomPlatform.Web.Api.Controllers {
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
        /// Provides a count of all existing vouchers.
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
                TotalVouchersRedeemed = result?.RedeemedCount ?? 0
            });
        }

        [HttpPost("vouchers/generated-redeemed-statistics")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchVouchersGeneratedAndRedeemedStats([FromBody] StatisticsRequestDto request) {
            try {
                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerSource(request.SourceId);

                var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                // Call separate methods or services to retrieve stats
                var response = await GenerationService.FetchTotalVouchersGeneratedAndRedeemedStats(parsedStartDate, parsedEndDate, request.SourceId, request.Latitude, request.Longitude, request.Radius);

                return Ok(response);
            }
            catch(ServiceProblemException e) {
                return StatusCode(e.HttpStatus, e.Message);
            }
        }

        [HttpPost("vouchers/consumed-statistics")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchVouchersConsumedStats([FromBody] StatisticsRequestDto request) {
            try {
                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerMerchant(request.MerchantId);

                var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                // Fetch the total amount of consumed vouchers
                var consumedVouchers =
                    await PaymentService.FetchTotalVouchersConsumedStats(parsedStartDate, parsedEndDate, request.MerchantId);

                // Return the JSON response
                return Ok(consumedVouchers);
            }
            catch(ServiceProblemException ex) {
                return StatusCode(ex.HttpStatus, ex.Message);
            }
        }


        /// <summary>
        /// Retrieves the total number of vouchers generated and redeemed within the specified date range and source.
        /// This is a POST request where filters are passed in the request body.
        /// </summary>
        /// <remarks>
        /// - This endpoint is restricted to admin users or authorized users of the source.
        /// - Date range and source ID can be provided via the request body.
        /// </remarks>
        /// <param name="request">Object containing startDate, endDate, and sourceId filters.</param>
        /// <returns>A JSON object containing total vouchers generated and redeemed.</returns>
        [HttpPost("vouchers/total-generated-redeemed")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchTotalVouchersGeneratedAndRedeemed(
            [FromBody] StatisticsRequestDto request
        ) {
            try {
                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerSource(request.SourceId);

                var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                // Fetch the total amount of generated and redeemed vouchers, passing the optional date range
                var generatedVouchers =
                    await GenerationService.FetchTotalVouchersGeneratedAndRedeemed(parsedStartDate, parsedEndDate,
                        request.SourceId);


                // Return the JSON response
                return Ok(new { TotalCount = generatedVouchers.TotalCount, RedeemedCount = generatedVouchers.RedeemedCount });
            }
            catch(ServiceProblemException ex) {
                return StatusCode(ex.HttpStatus, ex.Message);
            }
        }

        /// <summary>
        /// Gets the total amount of consumed vouchers within the specified date range.
        /// </summary>
        /// <remarks>
        /// This endpoint is restricted to admin users. If the user is not an admin, a 403 Forbidden status is returned.
        /// The date range can be modified to take parameters from the query string.
        /// </remarks>
        /// <returns>
        /// Returns a 200 OK status with the total number of consumed vouchers.
        /// If the user is not authorized, a 403 Forbidden status is returned.
        /// </returns>
        [HttpPost("vouchers/total-consumed")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchTotalVouchersConsumed(
            [FromBody] StatisticsRequestDto request
        ) {
            try {
                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerMerchant(request.MerchantId);

                var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                // Fetch the total amount of consumed vouchers
                var consumedVouchers =
                    await PaymentService.FetchTotalVouchersConsumed(parsedStartDate, parsedEndDate, request.MerchantId);

                // Return the JSON response
                return Ok(consumedVouchers);
            }
            catch(ServiceProblemException ex) {
                return StatusCode(ex.HttpStatus, ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the total number of vouchers generated, grouped by aim, within an optional date range or for a specific source.
        /// </summary>
        /// <remarks>
        /// - Restricted to admin users or owners of the source.
        /// - If the user is not authorized, a 403 Forbidden status is returned.
        /// - Optional filters include a date range (startDate, endDate) and source ID.
        /// - If no filters are provided, the statistics are calculated for the entire available dataset.
        /// </remarks>
        /// <param name="request">An object containing optional filters: startDate, endDate, and sourceId.</param>
        /// <returns>A JSON response containing the total vouchers generated, grouped by aim.</returns>
        /// <response code="200">Returns the statistics of generated vouchers grouped by aim.</response>
        /// <response code="403">If the user is not authorized to access the source data.</response>
        /// <response code="404">If the source is not found.</response>

        [HttpPost("vouchers/total-generated-by-aim")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchTotalVouchersGeneratedByAim(
            [FromBody] StatisticsRequestDto request
        ) {
            try {
                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerSource(request.SourceId);

                var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                var generatedVouchersByAim =
                    await GenerationService.FetchTotalVouchersGeneratedByAim(parsedStartDate, parsedEndDate, request.SourceId);
                return Ok(generatedVouchersByAim);
            }
            catch(ServiceProblemException e) {
                return StatusCode(e.HttpStatus, e.Message);
            }
        }

        /// <summary>
        /// Gets the list of consumed vouchers grouped by aim
        /// </summary>
        /// <remarks>
        /// This endpoint is restricted to admin users. If the user is not an admin, a 403 Forbidden status is returned.
        /// The date range can be modified to take parameters from the query string.
        /// </remarks>
        /// <returns>
        /// Returns a 200 OK status with the list.
        /// If the user is not authorized, a 403 Forbidden status is returned.
        /// </returns>
        [HttpPost("vouchers/total-consumed-by-aims")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchTotalVouchersConsumedByAim(
            [FromBody] StatisticsRequestDto request
        ) {
            try {
                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerMerchant(request.MerchantId);

                var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                // Fetch the list of consumed vouchers based on aim
                var listConsumedByAims =
                    await PaymentService.FetchTotalVouchersConsumedByAim(parsedStartDate, parsedEndDate, request.MerchantId);

                // Return consumed vouchers divided for period
                return Ok(listConsumedByAims);
            }
            catch(ServiceProblemException e) {
                return StatusCode(e.HttpStatus, e.Message);
            }
        }

        /// <summary>
        /// Retrieves a list of consumed vouchers grouped by offers for a merchant.
        /// This is a POST request where filters are passed in the request body.
        /// </summary>
        /// <remarks>
        /// - This endpoint is restricted to merchant users.
        /// - The request should contain an optional merchantId for which the vouchers were consumed.
        /// </remarks>
        /// <param name="request">Object containing the merchantId filter.</param>
        /// <returns>A list of consumed vouchers grouped by offer.</returns>
        [HttpPost("merchant/voucher/total-consumed-by-offer")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchConsumedVouchersByOffer(
            [FromBody] StatisticsRequestDto request
        ) {
            await IsUserAdminOrOwnerMerchant(request.MerchantId);
            // ********************************
            // TO ADD DATA FILTER ON SERVICE
            // ****************************
            if(request.MerchantId.HasValue) {
                // Fetch the list of consumed vouchers based on the merchant offer
                var listConsumedByOffer = await OfferService.FetchConsumedVouchersByOffer(request.MerchantId.Value);

                // Return consumed vouchers divided for period
                return Ok(listConsumedByOffer);
            }

            return (BadRequest());
        }

        /// <summary>
        /// Retrieves the rank of merchants based on the total amount of vouchers consumed in a specified period.
        /// This is a POST request where filters are passed in the request body.
        /// </summary>
        /// <remarks>
        /// - Restricted to merchant users.
        /// - The request should contain optional filters for date range.
        /// </remarks>
        /// <param name="request">Object containing startDate and endDate filters for ranking.</param>
        /// <returns>Merchants rank based on consumed vouchers.</returns>
        [HttpPost("merchant/rank-consumed")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchMerchantsRank(
            [FromBody] StatisticsRequestDto request
        ) {
            try {
                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerMerchant(request.MerchantId);

                var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                // Fetch the list of consumed vouchers based on aim
                var merchantRank = await PaymentService.GetMerchantRank(parsedStartDate, parsedEndDate, request.MerchantId);

                // Return consumed vouchers divided for period
                return Ok(merchantRank);
            }
            catch(ServiceProblemException e) {
                return StatusCode(e.HttpStatus, e.Message);
            }
        }

        /// <summary>
        /// Get the total number of unused vouchers by position
        /// </summary>
        [HttpPost("voucher/available")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> FetchVouchersAvailable(
            [FromBody] StatisticsRequestDto request
        ) {
            // Think how to make a control on this api
            await IsUserAdminOrOwnerMerchant(request.MerchantId);

            // Fetch the number of unused vouchers
            var numberUnusedVouchers = await GenerationService.FetchVouchersAvailable(request.Latitude, request.Longitude, request.Radius);

            // Return consumed vouchers divided for period
            return Ok(numberUnusedVouchers);
        }

        /// <summary>
        /// Retrieves the total number of vouchers generated over time based on optional filters such as date range and source.
        /// </summary>
        /// <param name="startDate">The start date for the date range filter (optional).</param>
        /// <param name="endDate">The end date for the date range filter (optional).</param>
        /// <param name="sourceId">The ID of the voucher source to filter by (optional).</param>
        /// <returns>The total number of vouchers generated within the specified criteria.</returns>
        [HttpPost("voucher/total-generated-redeemed-over-time")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetTotalGeneratedRedeemedVouchersOverTime(
            [FromBody] StatisticsRequestDto request
        ) {
            // check if user is admin or owner of the source
            await IsUserAdminOrOwnerSource(request.SourceId);

            var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

            // Fetch the list of consumed vouchers based on aim
            var totalGeneratedVouchersOverTime = await GenerationService.GetTotalGeneratedRedeemedVouchersOverTime(parsedStartDate, parsedEndDate, request.SourceId);
            return Ok(totalGeneratedVouchersOverTime);
        }

        /// <summary>
        /// Retrieves the total number of voucher usage over time based on optional filters like period of time and merchant.
        /// </summary>
        /// <param name="startDate">The start date to filter by (optional).</param>
        /// <param name="endDate">The end date to filter by (optional).</param>
        /// <param name="merchantId">The ID of the merchant to filter by (optional).</param>
        /// <returns>The total number of voucher consumed over time.</returns>
        [HttpPost("voucher/total-consumption-over-time")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetTotalConsumptionVouchersOverTime(
            [FromBody] StatisticsRequestDto request
        ) {
            // check if user is admin or owner of the source
            await IsUserAdminOrOwnerMerchant(request.MerchantId);

            var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

            // Fetch the list of consumed vouchers based on aim
            var totalConsumedVouchersOverTime = await PaymentService.GetTotalConsumedVouchersOverTime(parsedStartDate, parsedEndDate, request.MerchantId);

            return Ok(totalConsumedVouchersOverTime);
        }

        [HttpPost("download/csv")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadCsv([FromBody] StatisticsRequestDto request) {
            await VerifyUserIsAdmin(); // check if user is an admin

            // if dates present check dates are valid and in case parse them
            var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

            // create general API to call them and save the data
            // save the data to put on the CSV
            // use the CSV to save the data in a file
            // send the file back
            var records = CsvFileHelper.GenerateCsvContent(request);

            return File(records, "text/csv", "totalConsumedOverTime.csv");
        }
    }
}
