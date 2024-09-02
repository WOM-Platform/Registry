using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using WomPlatform.Web.Api.OutputModels.Stats;
using WomPlatform.Web.Api.Utilities;
using WomPlatform.Web.Api.DatabaseDocumentModels;

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
        [HttpGet("vouchers/total-generated-redeemed")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetTotalVouchersGeneratedRedeemed(
            [FromQuery] String startDate = null,
            [FromQuery] String endDate = null,
            [FromQuery] String instrumentName = null
        ) {
            DateTime? parsedStartDate = null;
            DateTime? parsedEndDate = null;

            // Check if the dates are provided and valid
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate)) {
                string format = "yyyy-MM-dd";

                // Try parsing the dates
                if (!DateTime.TryParseExact(startDate, format, null, System.Globalization.DateTimeStyles.None, out DateTime tempStartDate) ||
                    !DateTime.TryParseExact(endDate, format, null, System.Globalization.DateTimeStyles.None, out DateTime tempEndDate)) {
                    return BadRequest("Invalid date format. Please use 'yyyy-MM-dd'.");
                }

                // Check if start date is before end date
                DateRangeHelper.CheckDateValidity(tempStartDate, tempEndDate);

                parsedStartDate = tempStartDate;
                parsedEndDate = tempEndDate;
            }

            // Check if the user is an admin
            bool isAdmin = await this.IsUserAdmin();
            if (!isAdmin) {
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Only administrators can access this resource.");
            }
            /*
            ObjectId? instrumentId = null;

           if (!instrumentName.IsNullOrEmpty()) {
                Source instrument = await SourceService.GetInstrumentByNameAsync(instrumentName);
                instrumentId = instrument.Id;
            }*/

            // Fetch the total amount of generated and redeemed vouchers, passing the optional date range
            var generatedVouchers = await GenerationService.GetTotalAmountOfGeneratedRedeemedVouchers(parsedStartDate, parsedEndDate, instrumentName);

            // Check if the result is null or does not contain the expected field
            if (generatedVouchers == null || !generatedVouchers.Contains("totalCount")) {
                return NotFound("No vouchers found for the given date range.");
            }

            // Extract the totalAmountGenerated value
            var totalCount = generatedVouchers["totalCount"].AsInt32;
            var redeemedCount = generatedVouchers["redeemedCount"].AsInt32;

            // Return the JSON response
            return Ok(new { totalCount, redeemedCount });
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
        [HttpGet("vouchers/total-consumed")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetTotalAmountOfConsumedVouchers(
            [FromQuery] String startDate = null,
            [FromQuery] String endDate = null,
            [FromQuery] String merchantName = null
        ) {
            DateTime? parsedStartDate = null;
            DateTime? parsedEndDate = null;

            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate)) {
                string format = "yyyy-MM-dd";

                // Try parsing the dates
                if (!DateTime.TryParseExact(startDate, format, null, System.Globalization.DateTimeStyles.None, out DateTime tempStartDate) ||
                    !DateTime.TryParseExact(endDate, format, null, System.Globalization.DateTimeStyles.None, out DateTime tempEndDate)) {
                    return BadRequest("Invalid date format. Please use 'yyyy-MM-dd'.");
                }

                // Check if start date is before end date
                DateRangeHelper.CheckDateValidity(tempStartDate, tempEndDate);

                parsedStartDate = tempStartDate;
                parsedEndDate = tempEndDate;
            }

            // Check if the user is an admin
            bool isAdmin = await this.IsUserAdmin();
            if(!isAdmin) {
                return Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "Only administrators can access this resource.");
            }

            // Fetch the total amount of consumed vouchers
            var consumedVouchers = await PaymentService.GetTotalAmountOfConsumedVouchers(parsedStartDate, parsedEndDate, merchantName);

            // Check if the result is null or does not contain the expected field
            if(consumedVouchers == null || !consumedVouchers.Contains("totalAmount")) {
                return NotFound("No vouchers found for the given date range.");
            }

            // Extract the totalAmount value
            var totalAmountConsumed = consumedVouchers["totalAmount"].AsInt32;

            // Return the JSON response
            return Ok(new { totalAmountConsumed });
        }

        [HttpGet("vouchers/total-generated-by-aim")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetGeneratedVouchersByAim(
            [FromQuery] String startDate = null,
            [FromQuery] String endDate = null,
            [FromQuery] String instrumentName = null
            ) {

            DateTime? parsedStartDate = null;
            DateTime? parsedEndDate = null;

            // Check if the dates are provided and valid
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate)) {
                string format = "yyyy-MM-dd";

                // Try parsing the dates
                if (!DateTime.TryParseExact(startDate, format, null, System.Globalization.DateTimeStyles.None, out DateTime tempStartDate) ||
                    !DateTime.TryParseExact(endDate, format, null, System.Globalization.DateTimeStyles.None, out DateTime tempEndDate)) {
                    return BadRequest("Invalid date format. Please use 'yyyy-MM-dd'.");
                }

                // Check if start date is before end date
                DateRangeHelper.CheckDateValidity(tempStartDate, tempEndDate);

                parsedStartDate = tempStartDate;
                parsedEndDate = tempEndDate;
            }

            // Check if the user is an admin
            bool isAdmin = await this.IsUserAdmin();
            if(!isAdmin) {
                return Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "Only administrators can access this resource.");
            }

            var generatedVouchersByAim = await GenerationService.GetVoucherTotalsByAimAsync(parsedStartDate, parsedEndDate, instrumentName);
            return Ok(generatedVouchersByAim);
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
        [HttpGet("vouchers/total-consumed-by-aims")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetConsumedVouchersByAim(
            [FromQuery] String startDate = null,
            [FromQuery] String endDate = null,
            [FromQuery] String merchantName = null
        ) {
            DateTime? parsedStartDate = null;
            DateTime? parsedEndDate = null;

            // Check if the dates are provided and valid
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate)) {
                string format = "yyyy-MM-dd";

                // Try parsing the dates
                if (!DateTime.TryParseExact(startDate, format, null, System.Globalization.DateTimeStyles.None, out DateTime tempStartDate) ||
                    !DateTime.TryParseExact(endDate, format, null, System.Globalization.DateTimeStyles.None, out DateTime tempEndDate)) {
                    return BadRequest("Invalid date format. Please use 'yyyy-MM-dd'.");
                }

                // Check if start date is before end date
                DateRangeHelper.CheckDateValidity(tempStartDate, tempEndDate);

                parsedStartDate = tempStartDate;
                parsedEndDate = tempEndDate;
            }

            // Check if the user is an admin
            bool isAdmin = await this.IsUserAdmin();
            if(!isAdmin) {
                return Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "Only administrators can access this resource.");
            }

            // Fetch the list of consumed vouchers based on aim
            var listConsumedByAims = await PaymentService.GetConsumedVouchersByAims(parsedStartDate, parsedEndDate, merchantName);

            // If no vouchers are found, consider returning a 404 status
            if(listConsumedByAims == null || !listConsumedByAims.Any()) {
                return NotFound("No vouchers found for the given date range.");
            }


            Console.WriteLine(" ******** listConsumedByAims ");
            Console.WriteLine(listConsumedByAims.ToJson());



            // Return consumed vouchers divided for period
            return Ok(listConsumedByAims);
        }

        /// <summary>
        /// Gets the list of consumed vouchers grouped by merchant
        /// </summary>
        /// <remarks>
        /// This endpoint is restricted to admin users. If the user is not an admin, a 403 Forbidden status is returned.
        /// The date range can be modified to take parameters from the query string.
        /// </remarks>
        /// <returns>
        /// Returns a 200 OK status with the list.
        /// If the user is not authorized, a 403 Forbidden status is returned.
        /// </returns>
        [HttpGet("consumed-vouchers-merchants")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetListConsumedVouchersByMerchant(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate
        ) {
            // Check if start date is before end date
            DateRangeHelper.CheckDateValidity(startDate, endDate);

            // Check if the user is an admin
            bool isAdmin = await this.IsUserAdmin();
            if(!isAdmin) {
                return Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "Only administrators can access this resource.");
            }

            //DateTime startDate = new DateTime(2022, 07, 01);
            //DateTime endDate = new DateTime(2024, 07, 01);

            // Fetch the list of consumed vouchers for merchant
            var listConsumedByMerchant = await PaymentService.GetListConsumedByMerchants(startDate, endDate);


            // If no vouchers are found, consider returning a 404 status
            if(listConsumedByMerchant == null || !listConsumedByMerchant.Any()) {
                return NotFound("No vouchers found for the given date range.");
            }

            // Return consumed vouchers divided for period
            return Ok(listConsumedByMerchant);
        }

        /// <summary>
        /// Gets the list of consumed vouchers grouped by offer
        /// </summary>
        /// for merchant to know how offers are going
        [HttpGet("consumed-vouchers-offer")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetListConsumedVouchersByOffer(
            [FromQuery] string merchantId
        ) {
            // Fetch the list of consumed vouchers based on the merchant offer
            var listConsumedByOffer = await PaymentService.GetListConsumedByOffer(merchantId);


            // If no vouchers are found, consider returning a 404 status
            if(listConsumedByOffer == null || !listConsumedByOffer.Any()) {
                return NotFound("No vouchers found for the given date range.");
            }

            // Return consumed vouchers divided for period
            return Ok(listConsumedByOffer);
        }

        /// <summary>
        /// Gets the position of the merchant based on total amount of vouchers consumed in a period of time
        /// </summary>
        [HttpGet("consumed-merchant-rank")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetMerchantRank(
            [FromQuery] string merchantId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate
        ) {
            // Check if start date is before end date
            DateRangeHelper.CheckDateValidity(startDate, endDate);

            // Fetch the merchant position based on the others
            var merchantRank = await PaymentService.GetMerchantRank(merchantId, startDate, endDate);

            // If no vouchers are found, consider returning a 404 status
            if(merchantRank == null || !merchantRank.Any()) {
                return NotFound("Merchant was not found for the given date range.");
            }

            // Return consumed vouchers divided for period
            return Ok(merchantRank);
        }

        /// <summary>
        /// Get total amount of vouchers redeemed from all the sources
        /// </summary>
        [HttpGet("redeemed-aims")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetRedeemedAimList(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate
        ) {
            // Check if start date is before end date
            DateRangeHelper.CheckDateValidity(startDate, endDate);

            // Fetch the total amount of redeemed vouchers
            var totalAmountOfRedeemedVouchers = await GenerationService.GetRedeemedAimList(startDate, endDate);

            // If no vouchers are found, consider returning a 404 status
            if(totalAmountOfRedeemedVouchers == null || !totalAmountOfRedeemedVouchers.Any()) {
                return NotFound("No data found.");
            }

            // Return consumed vouchers divided for period
            return Ok(totalAmountOfRedeemedVouchers);
        }

        /// <summary>
        /// Get the total number of unused vouchers by position
        /// </summary>
        [HttpGet("unused-vouchers")]
        [Authorize]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetNumberOfUnusedVouchers(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] int radius
        ) {
            // Fetch the number of unused vouchers
            var numberUnusedVouchers = await GenerationService.GetNumberUnusedVouchers(latitude, longitude, radius);

            // If no vouchers are found, consider returning a 404 status
            if(numberUnusedVouchers == null || !numberUnusedVouchers.Any()) {
                return NotFound("No data found.");
            }

            // Return consumed vouchers divided for period
            return Ok(numberUnusedVouchers);
        }
    }
}
