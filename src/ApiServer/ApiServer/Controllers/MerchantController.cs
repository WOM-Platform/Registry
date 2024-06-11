using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
using WomPlatform.Web.Api.InputModels.Merchant;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Merchant;
using WomPlatform.Web.Api.OutputModels.Pos;
using WomPlatform.Web.Api.OutputModels.Source;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/merchant")]
    [OperationsTags("Merchant")]
    [RequireHttpsInProd]
    public class MerchantController : BaseRegistryController {

        public MerchantController(
            IServiceProvider serviceProvider,
            ILogger<MerchantController> logger)
        : base(serviceProvider, logger) {
        }

        private static readonly Regex AllZerosRegex = new("^0+$", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Registers a new merchant to the service.
        /// </summary>
        /// <param name="input">Merchant registration payload.</param>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(MerchantOutput), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(CreateMerchantInput input) {
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

            (User user, bool isAdmin) = await RequireLoggedUser();

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
                    Access = new(),
                    Enabled = true, // All merchants are automatically enabled for now
                    ActivationCode = input.ActivationCode.NormalizeCode(),
                };

                // Add user as Merchant administrator only if user is not platform administrator
                if(!isAdmin) {
                    merchant.Access.Add(new AccessControlEntry<MerchantRole> {
                        UserId = user.Id,
                        Role = MerchantRole.Admin,
                    });
                }

                await MerchantService.CreateMerchant(merchant);

                Logger.LogInformation("New merchant created {0} by user {1}", merchant.Id, user.Id);

                return CreatedAtAction(
                    nameof(GetInformation),
                    new {
                        merchantId = merchant.Id
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
        /// List merchants.
        /// </summary>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(Paged<MerchantOutput>), StatusCodes.Status200OK)]
        public async Task<ActionResult> ListMerchants(
            [FromQuery] string search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] [DefaultValue(MerchantService.MerchantListOrder.Name)] MerchantService.MerchantListOrder orderBy = MerchantService.MerchantListOrder.Name
        ) {
            (var user, bool isAdmin) = await RequireLoggedUser();
            ObjectId? userFilter = isAdmin ? null : user.Id;

            (var results, var count) = await MerchantService.ListMerchants(userFilter, search, page, pageSize, orderBy);

            return Ok(Paged<MerchantOutput>.FromPage(
                (from m in results select m.ToOutput()).ToArray(),
                page,
                pageSize,
                count
            ));
        }

        /// <summary>
        /// Retrieves information about an existing merchant.
        /// </summary>
        /// <param name="merchantId">Merchant ID.</param>
        /// <remarks>
        /// Can be accessed only if logged in user is the merchant's administrator or POS user.
        /// </remarks>
        [HttpGet("{merchantId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(MerchantOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInformation(
            [FromRoute] ObjectId merchantId
        ) {
            var existingMerchant = await MerchantService.GetMerchantById(merchantId);
            if(existingMerchant == null) {
                return NotFound();
            }

            return Ok(existingMerchant.ToOutput());
        }

        /// <summary>
        /// Merchant update payload.
        /// </summary>
        public record MerchantUpdateInput(
            [Required]
            [MinLength(8)]
            string Name,
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
            string Url,
            bool Enabled
        );

        /// <summary>
        /// Updates information about an existing merchant.
        /// </summary>
        /// <param name="merchantId">Merchant ID.</param>
        /// <param name="input">Updated information.</param>
        /// <remarks>
        /// Can be accessed only if logged in user is the merchant's administrator.
        /// </remarks>
        [HttpPut("{merchantId}")]
        [Authorize]
        [ProducesResponseType(typeof(MerchantOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateMerchant(
            [FromRoute] ObjectId merchantId,
            [FromBody] MerchantUpdateInput input
        ) {
            var existingMerchant = await VerifyUserIsAdminOfMerchant(merchantId);

            if(existingMerchant.Enabled != input.Enabled && !await IsUserAdmin()) {
                return Forbid();
            }

            try {
                existingMerchant.Name = input.Name;
                existingMerchant.PrimaryActivityType = input.PrimaryActivity;
                existingMerchant.Address = new AddressBlock {
                    StreetName = input.Address,
                    StreetNumber = input.StreetNumber,
                    ZipCode = input.ZipCode,
                    City = input.City,
                    Country = input.Country,
                    FormattedAddress = input.FormattedAddress,
                    GoogleMapsPlaceId = input.GoogleMapsPlaceId,
                };
                existingMerchant.Description = input.Description;
                existingMerchant.WebsiteUrl = input.Url;
                existingMerchant.Enabled = input.Enabled;
                existingMerchant.LastUpdate = DateTime.UtcNow;

                await MerchantService.ReplaceMerchant(existingMerchant);
            }
            catch(Exception) {
                Logger.LogError("Failed to update merchant {0}", merchantId);
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

        [HttpGet("{merchantId}/access")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(MerchantAccessOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetMerchantAccess(
            [FromRoute] ObjectId merchantId
        ) {
            var merchant = await VerifyUserIsAdminOfMerchant(merchantId);

            var userTasks = merchant.Access.ToSafeList().Select(async (AccessControlEntry<MerchantRole> entry) => {
                var user = await UserService.GetUserById(entry.UserId);
                return new MerchantAccessOutput.UserAccessInformation {
                    UserId = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Surname = user.Surname,
                    Role = entry.Role,
                };
            });
            var users = await Task.WhenAll(userTasks);

            return Ok(new MerchantAccessOutput {
                MerchantId = merchant.Id,
                MerchantName = merchant.Name,
                Users = users,
            });
        }

        [HttpPost("{merchantId}/access")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(SourceAccessOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GrantSourceAccess(
            [FromRoute] ObjectId merchantId,
            [FromQuery] ObjectId userId,
            [FromQuery] MerchantRole role = MerchantRole.Admin
        ) {
            var merchant = await VerifyUserIsAdminOfMerchant(merchantId);

            var user = await UserService.GetUserById(userId);
            if(user == null) {
                return this.UserNotFound();
            }

            merchant.Access = (from accessEntry in merchant.Access.ToSafeList()
                               where accessEntry.UserId != userId
                               select accessEntry)
                                .Concat([new AccessControlEntry<MerchantRole> {
                                    UserId = userId,
                                    Role = role,
                                }]).ToList();

            if(!await MerchantService.ReplaceMerchant(merchant)) {
                return this.WriteFailed("Failed to update merchant");
            }

            return this.NoContent();
        }

        [HttpDelete("{merchantId}/access/{userId}")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(SourceAccessOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RevokeSourceAccess(
            [FromRoute] ObjectId merchantId,
            [FromRoute] ObjectId userId
        ) {
            var merchant = await VerifyUserIsAdminOfMerchant(merchantId);

            merchant.Access = (from accessEntry in merchant.Access.ToSafeList()
                             where accessEntry.UserId != userId
                             select accessEntry)
                            .ToList();

            if(!await MerchantService.ReplaceMerchant(merchant)) {
                return this.WriteFailed("Failed to update merchant");
            }

            return this.NoContent();
        }

        /// <summary>
        /// Retrieves the list of POS of the merchant.
        /// </summary>
        /// <param name="id">Merchant ID.</param>
        /// <remarks>
        /// Can be accessed only if logged in user is the merchant's administrator or user.
        /// </remarks>
        [HttpGet("{merchantId}/pos")]
        [Authorize]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetPos(
            [FromRoute] ObjectId merchantId
        ) {
            var merchant = await this.VerifyUserIsUserOfMerchant(merchantId);

            var pos = await PosService.GetPosByMerchant(merchantId);

            return Ok(new MerchantPosOutput {
                MerchantId = merchant.Id,
                MerchantName = merchant.Name,
                Pos = (from p in pos
                       let pictureOutput = PicturesService.GetPosCoverOutput(p.CoverPath, p.CoverBlurHash)
                       select p.ToAuthOutput(pictureOutput)).ToArray(),
            });
        }

    }

}
