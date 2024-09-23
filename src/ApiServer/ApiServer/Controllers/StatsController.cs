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


        /// <summary>
        /// Gets the total amount of generated vouchers within the specified date range.
        /// </summary>
        /// <remarks>
        /// This endpoint is restricted to admin users. If the user is not an admin, a 403 Forbidden status is returned.
        /// The date range can be modified to take parameters from the query string.
        /// </remarks>
        /// <returns>
        /// Returns a 200 OK status with the total number of generated vouchers.
        /// If the user is not authorized, a 403 Forbidden status is returned.
        /// </returns>
        [HttpPost("vouchers/total-generated-redeemed")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetTotalVouchersGeneratedRedeemed(
            [FromBody] StatisticsRequestDto request
        ) {
            try {
                Console.WriteLine($"request: {request}");
                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerSource(request.SourceId);

                var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                // Fetch the total amount of generated and redeemed vouchers, passing the optional date range
                var generatedVouchers =
                    await GenerationService.GetTotalAmountOfGeneratedRedeemedVouchers(parsedStartDate, parsedEndDate,
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
        public async Task<ActionResult> GetTotalAmountOfConsumedVouchers(
            [FromBody] StatisticsRequestDto request
        ) {
            try {
                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerMerchant(request.MerchantId);

                var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                // Fetch the total amount of consumed vouchers
                var consumedVouchers =
                    await PaymentService.GetTotalAmountOfConsumedVouchers(parsedStartDate, parsedEndDate, request.MerchantId);

                // Return the JSON response
                return Ok(consumedVouchers);
            }
            catch(ServiceProblemException ex) {
                return StatusCode(ex.HttpStatus, ex.Message);
            }
        }

        [HttpPost("vouchers/total-generated-by-aim")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetGeneratedVouchersByAim(
            [FromBody] StatisticsRequestDto request
        ) {
            try {
                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerSource(request.SourceId);

                var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                var generatedVouchersByAim =
                    await GenerationService.GetVoucherTotalsByAimAsync(parsedStartDate, parsedEndDate, request.SourceId);
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
        public async Task<ActionResult> GetConsumedVouchersByAim(
            [FromBody] StatisticsRequestDto request
        ) {
            try {
                // check if user is admin or owner of the source
                await IsUserAdminOrOwnerMerchant(request.MerchantId);

                var (parsedStartDate, parsedEndDate) = DateRangeHelper.ParseAndValidateDates(request.StartDate, request.EndDate);

                // Fetch the list of consumed vouchers based on aim
                var listConsumedByAims =
                    await PaymentService.GetConsumedVouchersByAims(parsedStartDate, parsedEndDate, request.MerchantId);

                // Return consumed vouchers divided for period
                return Ok(listConsumedByAims);
            }
            catch(ServiceProblemException e) {
                return StatusCode(e.HttpStatus, e.Message);
            }
        }

        /// <summary>
        /// Gets the list of consumed vouchers grouped by offer
        /// </summary>
        /// for merchant to know how offers are going
        [HttpPost("merchant/voucher/total-consumed-by-offer")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetListConsumedVouchersByOffer(
            [FromBody] StatisticsRequestDto request
        ) {
            // ********************************
            // TO ADD DATA FILTER ON SERVICE
            // ****************************
            if(request.MerchantId.HasValue) {
                // Fetch the list of consumed vouchers based on the merchant offer
                var listConsumedByOffer = await OfferService.GetListConsumedByOffer(request.MerchantId.Value);

                // Return consumed vouchers divided for period
                return Ok(listConsumedByOffer);
            }

            return (BadRequest());
        }

        /// <summary>
        /// Gets the position of the merchant based on total amount of vouchers consumed in a period of time
        /// </summary>
        [HttpPost("merchant/rank-consumed")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetMerchantRank(
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
        public async Task<ActionResult> GetNumberOfAvailableVouchers(
            [FromQueryAttribute] double? latitude = null,
            [FromQueryAttribute] double? longitude = null,
            [FromQueryAttribute] int? radius = null,
            [FromQueryAttribute] ObjectId? merchantId = null
        ) {
            // Think how to make a control on this api
            await IsUserAdminOrOwnerMerchant(merchantId);

            // Fetch the number of unused vouchers
            var numberUnusedVouchers = await GenerationService.GetNumberAvailableVouchers(latitude, longitude, radius);

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

        [HttpGet("download/csv")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public IActionResult DownloadCsv() {
            var records = CsvFileHelper.GenerateCsvContent();

            return File(records, "text/csv", "totalConsumedOverTime.csv");
        }
    }
}
