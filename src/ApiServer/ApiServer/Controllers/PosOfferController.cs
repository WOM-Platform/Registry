using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels;
using WomPlatform.Web.Api.InputModels.Offers;
using WomPlatform.Web.Api.OutputModels.Offers;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/pos")]
    [OperationsTags("Point of service")]
    [RequireHttpsInProd]
    public class PosOfferController : BaseRegistryController {

        public PosOfferController(IServiceProvider serviceProvider, ILogger<PosOfferController> logger)
        : base(serviceProvider, logger) {
        }

        /// <summary>
        /// Retrieves the offers of a POS.
        /// </summary>
        /// <param name="posId">POS ID.</param>
        [HttpGet("{posId}/offers")]
        [ProducesResponseType(typeof(OfferOutput[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPosOffers(
            [FromRoute] ObjectId posId
        ) {
            var pos = await PosService.GetPosById(posId);
            if(pos == null) {
                return NotFound();
            }

            var results = await OfferService.GetOffersOfPos(pos.Id);

            return Ok((from result in results select result.ToDetailsOutput()).ToArray());
        }

        /// <summary>
        /// Create a new offer tied to a POS.
        /// </summary>
        /// <param name="posId">POS ID.</param>
        [HttpPost("{posId}/offers")]
        [Authorize]
        [ProducesResponseType(typeof(OfferOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddNewOffer(
            [FromRoute] ObjectId posId,
            [FromBody] OfferRegistrationInput input
        ) {
            Logger.LogInformation("Adding new offer “{0}” for POS {1}", input.Title, posId);

            (var merchant, var pos) = await VerifyUserIsAdminOfPos(posId);

            try {
                Filter filter = input.Filter.ToDocument();
                var paymentRequest = await PaymentService.CreatePaymentRequest(pos, input.Cost, filter, isPersistent: true, isPreVerified: true);
                Logger.LogDebug("Created payment request {0} for new offer", paymentRequest.Otc);

                var offer = new Offer {
                    Title = input.Title,
                    Description = input.Description,
                    Payment = new Offer.PaymentInformation {
                        Otc = paymentRequest.Otc,
                        Password = paymentRequest.Password,
                        Cost = paymentRequest.Amount,
                        Filter = paymentRequest.Filter,
                    },
                    Pos = new Offer.PosInformation {
                        Id = pos.Id,
                        Name = pos.Name,
                        Description = pos.Description,
                        CoverPath = pos.CoverPath,
                        CoverBlurHash = pos.CoverBlurHash,
                        Position = pos.Position,
                        Url = pos.Url,
                    },
                    Merchant = new Offer.MerchantInformation {
                        Id = merchant.Id,
                        Name = merchant.Name,
                        WebsiteUrl = merchant.WebsiteUrl,
                    },
                    CreatedOn = DateTime.UtcNow,
                    LastUpdate = DateTime.UtcNow,
                    Deactivated = !pos.IsActive,
                };
                await OfferService.AddOffer(offer);
                Logger.LogInformation("Created new offer {0} for POS {1}", offer.Id, posId);

                return Ok(offer.ToDetailsOutput());
            }
            catch(Exception) {
                Logger.LogError("Failed to persist offer");
                throw;
            }
        }

        /// <summary>
        /// Retrieve an offer.
        /// </summary>
        [HttpGet("{posId}/offers/{offerId}")]
        [ProducesResponseType(typeof(OfferOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPosOffer(
            [FromRoute] ObjectId posId,
            [FromRoute] ObjectId offerId
        ) {
            var offer = await OfferService.GetOfferById(offerId);
            if(offer == null) {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: $"Offer {offerId} does not exist");
            }
            if(offer.Pos.Id != posId) {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: $"Offer {offerId} is not owned by POS {posId}");
            }

            return Ok(offer.ToDetailsOutput());
        }

        /// <summary>
        /// Overwrites an offer tied to a POS.
        /// </summary>
        /// <param name="posId">POS ID.</param>
        [HttpPut("{posId}/offers/{offerId}")]
        [Authorize]
        [ProducesResponseType(typeof(OfferOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> OverwriteOffer(
            [FromRoute] ObjectId posId,
            [FromRoute] ObjectId offerId,
            [FromBody] OfferRegistrationInput input
        ) {
            (var merchant, var pos) = await VerifyUserIsAdminOfPos(posId);

            var existingOffer = await OfferService.GetOfferById(offerId);
            if(existingOffer == null) {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: $"Offer {offerId} does not exist");
            }
            if(existingOffer.Pos.Id != posId) {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: $"Offer {offerId} is not owned by POS {posId}");
            }

            try {
                Filter filter = input.Filter.ToDocument();
                var paymentRequest = await PaymentService.CreatePaymentRequest(pos, input.Cost, filter, isPersistent: true, isPreVerified: true);
                Logger.LogDebug("Created new payment request {0} for offer", paymentRequest.Otc);

                var replacementOffer = new Offer {
                    Id = existingOffer.Id,
                    Title = input.Title,
                    Description = input.Description,
                    Payment = new Offer.PaymentInformation {
                        Otc = paymentRequest.Otc,
                        Password = paymentRequest.Password,
                        Cost = paymentRequest.Amount,
                        Filter = paymentRequest.Filter,
                    },
                    Pos = new Offer.PosInformation {
                        Id = pos.Id,
                        Name = pos.Name,
                        Description = pos.Description,
                        CoverPath = pos.CoverPath,
                        CoverBlurHash = pos.CoverBlurHash,
                        Position = pos.Position,
                        Url = pos.Url,
                    },
                    Merchant = new Offer.MerchantInformation {
                        Id = merchant.Id,
                        Name = merchant.Name,
                        WebsiteUrl = merchant.WebsiteUrl,
                    },
                    CreatedOn = existingOffer.CreatedOn,
                    LastUpdate = DateTime.UtcNow,
                    Deactivated = !pos.IsActive,
                };
                await OfferService.ReplaceOffer(replacementOffer);
                Logger.LogInformation("Offer {0} for POS {1} has been replaced", replacementOffer.Id, posId);

                return Ok(replacementOffer.ToDetailsOutput());
            }
            catch(Exception) {
                Logger.LogError("Failed to persist offer");
                throw;
            }
        }

        /// <summary>
        /// Overwrites an offer's title and description.
        /// </summary>
        [HttpPut("{posId}/offers/{offerId}/title")]
        [Authorize]
        [ProducesResponseType(typeof(OfferOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> OverwriteOfferTitle(
            [FromRoute] ObjectId posId,
            [FromRoute] ObjectId offerId,
            [FromBody] OfferDescriptionInput input
        ) {
            (_, _) = await VerifyUserIsAdminOfPos(posId);

            var existingOffer = await OfferService.GetOfferById(offerId);
            if(existingOffer == null) {
                return NotFound();
            }
            if(existingOffer.Pos.Id != posId) {
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: $"Offer {offerId} is not owned by POS {posId}");
            }

            try {
                await OfferService.UpdateOfferDescription(offerId, input.Title, input.Description);

                return AcceptedAtAction(nameof(GetPosOffer), new { posId = posId, offerId = offerId }, null);
            }
            catch(Exception) {
                Logger.LogError("Failed to persist offer");
                throw;
            }
        }

        public record OfferDeactivationStatusOutput(bool Deactivated);

        /// <summary>
        /// Gets an offer's deactivation status.
        /// </summary>
        [HttpGet("{posId}/offers/{offerId}/deactivation")]
        [Authorize]
        [ProducesResponseType(typeof(OfferDeactivationStatusOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOfferDeactivationStatus(
            [FromRoute] ObjectId posId,
            [FromRoute] ObjectId offerId
        ) {
            (_, _) = await VerifyUserIsAdminOfPos(posId);

            var existingOffer = await OfferService.GetOfferById(offerId);
            if(existingOffer == null) {
                return NotFound();
            }
            if(existingOffer.Pos.Id != posId) {
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: $"Offer {offerId} is not owned by POS {posId}");
            }

            return Ok(new OfferDeactivationStatusOutput(existingOffer.Deactivated));
        }

        public record OfferDeactivationStatusInput(bool Deactivated);

        /// <summary>
        /// Sets an offer's deactivation status.
        /// </summary>
        [HttpPut("{posId}/offers/{offerId}/deactivation")]
        [Authorize]
        [ProducesResponseType(typeof(OfferDeactivationStatusOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetOfferDeactivationStatus(
            [FromRoute] ObjectId posId,
            [FromRoute] ObjectId offerId,
            [FromBody] OfferDeactivationStatusInput input
        ) {
            (_, _) = await VerifyUserIsAdminOfPos(posId);

            var existingOffer = await OfferService.GetOfferById(offerId);
            if(existingOffer == null) {
                return NotFound();
            }
            if(existingOffer.Pos.Id != posId) {
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: $"Offer {offerId} is not owned by POS {posId}");
            }

            try {
                await OfferService.DeactivateOffer(offerId, input.Deactivated);

                return Ok(new OfferDeactivationStatusOutput(input.Deactivated));
            }
            catch(Exception) {
                Logger.LogError("Failed to update offer");
                throw;
            }
        }

        /// <summary>
        /// Delete an offer.
        /// </summary>
        [HttpDelete("{posId}/offers/{offerId}")]
        [Authorize]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePosOffer(
            [FromRoute] ObjectId posId,
            [FromRoute] ObjectId offerId
        ) {
            (_, _) = await VerifyUserIsAdminOfPos(posId);

            var offer = await OfferService.GetOfferById(offerId);
            if(offer == null) {
                return NotFound();
            }
            if(offer.Pos.Id != posId) {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: $"Offer {offerId} is not owned by POS {posId}");
            }

            await OfferService.DeleteOffer(offerId);

            return Ok();
        }
    }

}
