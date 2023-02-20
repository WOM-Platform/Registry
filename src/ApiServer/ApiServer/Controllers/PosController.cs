using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels;
using WomPlatform.Web.Api.InputModels.Offers;
using WomPlatform.Web.Api.InputModels.Pos;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Offers;
using WomPlatform.Web.Api.OutputModels.Pos;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/pos")]
    [RequireHttpsInProd]
    [OperationsTags("Point of service")]
    public class PosController : BaseRegistryController {

        public PosController(IServiceProvider serviceProvider, ILogger<AdminController> logger)
        : base(serviceProvider, logger) {
        }

        /// <summary>
        /// Registers a new POS to the service.
        /// </summary>
        /// <param name="input">POS registration payload.</param>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(PosOutput), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(
            [FromBody] PosRegistrationInput input
        ) {
            var owningMerchant = await MerchantService.GetMerchantById(input.OwnerMerchantId);
            if(owningMerchant == null) {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "Owning merchant does not exist");
            }

            if(!await VerifyUserIsAdminOfMerchant(owningMerchant)) {
                return Forbid();
            }

            if((!input.Latitude.HasValue || !input.Longitude.HasValue) && string.IsNullOrWhiteSpace(input.Url)) {
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "POS without geographic position must have an URL");
            }

            try {
                var posKeys = CryptoHelper.CreateKeyPair();

                var pos = new Pos {
                    MerchantId = owningMerchant.Id,
                    Name = input.Name,
                    Description = input.Description,
                    Position = (input.Latitude.HasValue && input.Longitude.HasValue) ?
                        new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(input.Longitude.Value, input.Latitude.Value)) :
                        null,
                    Url = string.IsNullOrWhiteSpace(input.Url) ? null : input.Url,
                    PrivateKey = posKeys.Private.ToPemString(),
                    PublicKey = posKeys.Public.ToPemString(),
                    CreatedOn = DateTime.UtcNow,
                    IsDummy = false,
                    IsActive = true,
                };
                await PosService.CreatePos(pos);

                return CreatedAtAction(
                    nameof(GetPosInformation),
                    new {
                        id = pos.Id
                    },
                    pos.ToOutput(PicturesService.DefaultPosCover)
                );
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to register new POS");
                throw;
            }
        }

        /// <summary>
        /// Retrieves information about an existing POS.
        /// </summary>
        /// <param name="posId">POS ID.</param>
        [HttpGet("{posId}")]
        [ProducesResponseType(typeof(PosOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPosInformation(
            [FromRoute] ObjectId posId
        ) {
            var pos = await PosService.GetPosById(posId);
            if(pos == null) {
                return NotFound();
            }

            var picCover = PicturesService.GetPosCoverOutput(pos.CoverPath, pos.CoverBlurHash);

            return Ok(pos.ToOutput(picCover));
        }

        /// <summary>
        /// Retrieves a list of POS.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Paged<PosOutput>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(
            [FromQuery] string search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] [DefaultValue(PosService.PosListOrder.Name)] PosService.PosListOrder orderBy = PosService.PosListOrder.Name
        ) {
            (var results, var count) = await PosService.ListPos(search, page, pageSize, orderBy);

            return Ok(Paged<PosOutput>.FromPage(
                (from pos in results
                 let picCover = PicturesService.GetPosCoverOutput(pos.CoverPath, pos.CoverBlurHash)
                 select pos.ToOutput(picCover)).ToArray(),
                page,
                pageSize,
                count
            ));
        }

        /// <summary>
        /// Retrieves a list of POS nearby a geographic point.
        /// </summary>
        [HttpGet("nearby")]
        [ProducesResponseType(typeof(Paged<PosOutput>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        ) {
            (var results, var count) = await PosService.ListPosByDistance(latitude, longitude, page, pageSize);

            return Ok(Paged<PosOutput>.FromPage(
                (from pos in results
                 let picCover = PicturesService.GetPosCoverOutput(pos.CoverPath, pos.CoverBlurHash)
                 select pos.ToOutput(picCover)).ToArray(),
                page,
                pageSize,
                count
            ));
        }

        /// <summary>
        /// Updates information about an existing POS.
        /// </summary>
        /// <param name="posId">Merchant ID.</param>
        /// <param name="input">Updated information.</param>
        [HttpPut("{posId}")]
        [Authorize]
        [ProducesResponseType(typeof(PosOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            [FromRoute] ObjectId posId,
            [FromBody] PosUpdateInput input
        ) {
            var pos = await PosService.GetPosById(posId);
            if(pos == null) {
                return NotFound();
            }

            var merchant = await MerchantService.GetMerchantById(pos.MerchantId);
            if(merchant == null) {
                Logger.LogError("Owning merchant {0} for POS {1} does not exist", pos.MerchantId, pos.Id);
                return NotFound();
            }

            if(!await VerifyUserIsAdminOfMerchant(merchant)) {
                return Forbid();
            }

            try {
                pos.Name = input.Name;
                pos.Description = input.Description;
                pos.Position = (input.Latitude.HasValue && input.Longitude.HasValue) ?
                    new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(input.Longitude.Value, input.Latitude.Value)) :
                    null;
                pos.Url = input.Url;
                pos.IsActive = input.IsActive;
                pos.LastUpdate = DateTime.UtcNow;

                await PosService.ReplacePos(pos);

                await OfferService.UpdatePosInformation(pos.Id, pos.Name, pos.Description, pos.Position?.Coordinates.Latitude, pos.Position?.Coordinates.Longitude, pos.Url, pos.IsActive);
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to update POS {0}", posId);
                throw;
            }

            var picPosCover = PicturesService.GetPosCoverOutput(pos.CoverPath, pos.CoverBlurHash);

            return Ok(pos.ToOutput(picPosCover));
        }

        /// <summary>
        /// Updates the cover of an existing POS.
        /// </summary>
        [HttpPost("{posId}/cover")]
        [Authorize]
        [DisableRequestSizeLimit]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(PosOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCover(
            [FromRoute] ObjectId posId,
            [FromForm] [Required] IFormFile image
        ) {
            (bool allowed, var errorResult, var merchant, var pos) = await VerifyUserIsAdminOfPos(posId);
            if(!allowed) {
                return errorResult;
            }

            // Safety checks on uploaded file
            if(image == null || image.Length == 0) {
                Logger.LogError("Image field null or empty");
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Image field null or empty");
            }
            if(image.Length > 4 * 1024 * 1024) {
                Logger.LogError("Image too large ({0} bytes)", image.Length);
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Image too large");
            }

            try {
                var posUrl = string.Format("{0}-{1}", merchant.City, pos.Name).ToCleanUrl();

                // Process and upload image
                using var stream = new MemoryStream();
                await image.CopyToAsync(stream);
                (var picturePath, var pictureBlurHash) = await PicturesService.ProcessAndUploadPicture(stream, posUrl, PicturesService.PictureUsage.PosCover);

                await PosService.UpdatePosCover(posId, picturePath, pictureBlurHash);
                await OfferService.UpdatePosCovers(posId, picturePath, pictureBlurHash);

                var picPosCover = PicturesService.GetPosCoverOutput(picturePath, pictureBlurHash);
                return Ok(pos.ToOutput(picPosCover));
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to update POS {0}", posId);
                throw;
            }
        }

        /// <summary>
        /// Deletes the cover of an existing POS.
        /// </summary>
        [HttpDelete("{posId}/cover")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(PosOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCover(
            [FromRoute] ObjectId posId
        ) {
            var pos = await PosService.GetPosById(posId);
            if(pos == null) {
                return NotFound();
            }

            var merchant = await MerchantService.GetMerchantById(pos.MerchantId);
            if(merchant == null) {
                Logger.LogError("Owning merchant {0} for POS {1} does not exist", pos.MerchantId, pos.Id);
                return NotFound();
            }

            if(!await VerifyUserIsAdminOfMerchant(merchant)) {
                return Forbid();
            }

            try {
                await PosService.UpdatePosCover(posId, null, null);
                await OfferService.UpdatePosCovers(posId, null, null);

                return Ok(pos.ToOutput(null));
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to update POS {0}", posId);
                throw;
            }
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
            (bool allowed, var errorResult, var merchant, var pos) = await VerifyUserIsAdminOfPos(posId);
            if(!allowed) {
                return errorResult;
            }

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
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to persist offer");
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
            (bool allowed, var errorResult, var merchant, var pos) = await VerifyUserIsAdminOfPos(posId);
            if(!allowed) {
                return errorResult;
            }

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
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to persist offer");
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
            (bool allowed, var result, _, _) = await VerifyUserIsAdminOfPos(posId);
            if(!allowed) {
                return result;
            }

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
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to persist offer");
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
            (bool allowed, var result, _, _) = await VerifyUserIsAdminOfPos(posId);
            if(!allowed) {
                return result;
            }

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
