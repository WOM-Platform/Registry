using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Merchant;
using WomPlatform.Web.Api.OutputModels.Pos;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/merchant")]
    [RequireHttpsInProd]
    [OperationsTags("Merchant")]
    public class MerchantController : BaseRegistryController {

        public MerchantController(
            IServiceProvider serviceProvider,
            ILogger<MerchantController> logger)
        : base(serviceProvider, logger) {
        }

        private static readonly Regex AllZerosRegex = new Regex("^0+$", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Merchant registration payload.
        /// </summary>
        public record MerchantRegisterInput(
            [Required]
            [MinLength(8)]
            string Name,
            [Required]
            [MinLength(11)] // Length of Partita IVA
            [MaxLength(16)] // Length of Codice Fiscale
            string FiscalCode,
            [Required]
            MerchantActivityType PrimaryActivity,
            [Required]
            string Address,
            string StreetNumber,
            [Required]
            string ZipCode,
            [Required]
            string City,
            [Required]
            string Country,
            string FormattedAddress,
            string GoogleMapsPlaceId,
            string Description,
            [Url]
            string Url
        );

        /// <summary>
        /// Registers a new merchant to the service.
        /// </summary>
        /// <param name="input">Merchant registration payload.</param>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(MerchantOutput), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(MerchantRegisterInput input) {
            Logger.LogInformation("Attempting to register new merchant {0} with fiscal code {1}", input.Name, input.FiscalCode);

            if(AllZerosRegex.IsMatch(input.FiscalCode)) {
                Logger.LogInformation("Fiscal code {0} is not valid", input.FiscalCode);

                return Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Supplied fiscal code is not valid",
                    type: "https://wom.social/api/problems/fiscal-code-not-valid"
                );
            }

            var existingMerchant = await MerchantService.GetMerchantByFiscalCode(input.FiscalCode);
            if(existingMerchant != null) {
                Logger.LogInformation("Fiscal code {0} already used for merchant {1}", input.FiscalCode, existingMerchant.Id);

                return Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Supplied fiscal code is already registered by merchant",
                    type: "https://wom.social/api/problems/fiscal-code-already-in-use-by-merchant"
                );
            }

            if(!User.GetUserId(out var loggedUserId)) {
                return Forbid();
            }

            try {
                var merchant = new Merchant {
                    Name = input.Name,
                    FiscalCode = input.FiscalCode.ToUpperInvariant(),
                    PrimaryActivityType = input.PrimaryActivity,
                    Address = new AddressBlock {
                        StreetName = input.Address,
                        StreetNumber = input.StreetNumber,
                        ZipCode = input.ZipCode,
                        City = input.City,
                        Country = input.Country,
                        FormattedAddress = input.FormattedAddress,
                        GoogleMapsPlaceId = input.GoogleMapsPlaceId,
                    },
                    Description = input.Description,
                    WebsiteUrl = input.Url,
                    CreatedOn = DateTime.UtcNow,
                    Access = new() {
                        new AccessControlEntry<MerchantRole> {
                            UserId = loggedUserId,
                            Role = MerchantRole.Admin,
                        }
                    },
                    Enabled = true // All merchants are automatically enabled for now
                };
                await MerchantService.CreateMerchant(merchant);

                Logger.LogInformation("New merchant created {0} by user {1}", merchant.Id, loggedUserId);

                return CreatedAtAction(
                    nameof(GetInformation),
                    new {
                        id = merchant.Id
                    },
                    merchant.ToOutput()
                );
            }
            catch(Exception) {
                Logger.LogError("Failed to register new merchant with fiscal code {0}", input.FiscalCode);
                throw;
            }
        }

        /// <summary>
        /// Retrieves information about an existing merchant.
        /// </summary>
        /// <param name="id">Merchant ID.</param>
        /// <remarks>
        /// Can be accessed only if logged in user is the merchant's administrator or POS user.
        /// </remarks>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MerchantOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInformation(
            [FromRoute] ObjectId id
        ) {
            var existingMerchant = await MerchantService.GetMerchantById(id);
            if(existingMerchant == null) {
                return NotFound();
            }

            return Ok(existingMerchant.ToOutput());
        }

        /// <summary>
        /// Merchant update payload.
        /// </summary>
        public record MerchantUpdateInput(
            [MinLength(8)]
            string Name,
            MerchantActivityType? PrimaryActivity,
            string Address,
            string StreetNumber,
            string ZipCode,
            string City,
            string Country,
            string FormattedAddress,
            string GoogleMapsPlaceId,
            string Description,
            [Url]
            string Url,
            bool Enabled
        );

        /// <summary>
        /// Updates information about an existing merchant.
        /// </summary>
        /// <param name="id">Merchant ID.</param>
        /// <param name="input">Updated information.</param>
        /// <remarks>
        /// Can be accessed only if logged in user is the merchant's administrator.
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(MerchantOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateMerchant(
            [FromRoute] ObjectId id,
            [FromBody] MerchantUpdateInput input
        ) {
            var existingMerchant = await VerifyUserIsAdminOfMerchant(id);

            try {
                if(input.Name != null) {
                    existingMerchant.Name = input.Name;
                }
                if(input.PrimaryActivity.HasValue) {
                    existingMerchant.PrimaryActivityType = input.PrimaryActivity.Value;
                }

                if(existingMerchant.Address == null) {
                    existingMerchant.Address = new AddressBlock();
                }
                if(input.Address != null) {
                    existingMerchant.Address.StreetName = input.Address;
                }
                if(input.StreetNumber != null) {
                    existingMerchant.Address.StreetNumber = input.StreetNumber;
                }
                if(input.ZipCode != null) {
                    existingMerchant.Address.ZipCode = input.ZipCode;
                }
                if(input.City != null) {
                    existingMerchant.Address.City = input.City;
                }
                if(input.Country != null) {
                    existingMerchant.Address.Country = input.Country;
                }
                if(input.FormattedAddress != null) {
                    existingMerchant.Address.FormattedAddress = input.FormattedAddress;
                }
                if(input.GoogleMapsPlaceId != null) {
                    existingMerchant.Address.GoogleMapsPlaceId = input.GoogleMapsPlaceId;
                }

                if(input.Description != null) {
                    existingMerchant.Description = input.Description;
                }
                if(input.Url != null) {
                    existingMerchant.WebsiteUrl = input.Url;
                }
                existingMerchant.LastUpdate = DateTime.UtcNow;
                existingMerchant.Enabled = input.Enabled;

                await MerchantService.ReplaceMerchant(existingMerchant);
            }
            catch(Exception) {
                Logger.LogError("Failed to update merchant {0}", id);
                throw;
            }

            return Ok(existingMerchant.ToOutput());
        }

        [HttpDelete("{merchantId}")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(MerchantDeleteOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(
            [FromRoute] ObjectId merchantId,
            [FromQuery] bool dryRun = false
        ) {
            Logger.LogInformation("Deleting merchant {0} ({1})", merchantId, dryRun ? "dry run" : "effective run");

            _ = await VerifyUserIsAdminOfMerchant(merchantId);

            var poses = await PosService.GetPosByMerchant(merchantId);
            if(dryRun) {
                long countOffers = 0;
                foreach(var pos in poses) {
                    countOffers += await OfferService.CountActiveOffersOfPos(pos.Id);
                }

                return Ok(new MerchantDeleteOutput {
                    OperationPerformed = false,
                    CountOfDeletedMerchants = 1,
                    CountOfDeletedPos = poses.Count,
                    CountOfDeletedOffers = countOffers,
                });
            }
            else {
                long countOffers = 0;
                foreach(var pos in poses) {
                    countOffers += await OfferService.DeleteOffersByPos(pos.Id);
                    await PosService.DeletePos(pos.Id);
                }

                await MerchantService.DeleteMerchant(merchantId);

                return Ok(new MerchantDeleteOutput {
                    OperationPerformed = true,
                    CountOfDeletedMerchants = 1,
                    CountOfDeletedPos = poses.Count,
                    CountOfDeletedOffers = countOffers,
                });
            }
        }

    }

}
