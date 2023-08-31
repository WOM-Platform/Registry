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
using WomPlatform.Web.Api.InputModels.Pos;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Pos;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/pos")]
    [OperationsTags("Point of service")]
    [RequireHttpsInProd]
    public class PosController : BaseRegistryController {

        public PosController(IServiceProvider serviceProvider, ILogger<PosController> logger)
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
        public async Task<ActionResult> Register(
            [FromBody] PosRegistrationInput input
        ) {
            var owningMerchant = await VerifyUserIsAdminOfMerchant(input.OwnerMerchantId);

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
                    Address = new AddressBlock {
                        StreetName = input.Address?.StreetName,
                        StreetNumber = input.Address?.StreetNumber,
                        ZipCode = input.Address?.ZipCode,
                        City = input.Address?.City,
                        Country = input.Address?.Country,
                        FormattedAddress = input.Address?.FormattedAddress,
                        GoogleMapsPlaceId = input.Address?.GoogleMapsPlaceId,
                    },
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
                        posId = pos.Id
                    },
                    pos.ToOutput(PicturesService.DefaultPosCover)
                );
            }
            catch(Exception) {
                Logger.LogError("Failed to register new POS");
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
        public async Task<ActionResult> GetPosInformation(
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
        public async Task<ActionResult> List(
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
        /// Retrieves a list of virtual POS.
        /// </summary>
        [HttpGet("virtual")]
        [ProducesResponseType(typeof(Paged<PosOutput>), StatusCodes.Status200OK)]
        public async Task<ActionResult> ListVirtual(
            [FromQuery] string search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] [DefaultValue(PosService.PosListOrder.Name)] PosService.PosListOrder orderBy = PosService.PosListOrder.Name
        ) {
            (var results, var count) = await PosService.ListPos(search, page, pageSize, orderBy, false);

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
        public async Task<ActionResult> List(
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
        public async Task<ActionResult> Update(
            [FromRoute] ObjectId posId,
            [FromBody] PosUpdateInput input
        ) {
            (_, var pos) = await VerifyUserIsAdminOfPos(posId);

            if((!input.Latitude.HasValue || !input.Longitude.HasValue) && string.IsNullOrWhiteSpace(input.Url)) {
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "POS without geographic position must have an URL");
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
            catch(Exception) {
                Logger.LogError("Failed to update POS {0}", posId);
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
        public async Task<ActionResult> UpdateCover(
            [FromRoute] ObjectId posId,
            [FromForm] [Required] IFormFile image
        ) {
            (var merchant, var pos) = await VerifyUserIsAdminOfPos(posId);

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
                var posUrl = string.Format("{0}-{1}", merchant.Address.City, pos.Name).ToCleanUrl();

                // Process and upload image
                using var stream = new MemoryStream();
                await image.CopyToAsync(stream);
                (var picturePath, var pictureBlurHash) = await PicturesService.ProcessAndUploadPicture(stream, posUrl, PicturesService.PictureUsage.PosCover);

                await PosService.UpdatePosCover(posId, picturePath, pictureBlurHash);
                await OfferService.UpdatePosCovers(posId, picturePath, pictureBlurHash);

                var picPosCover = PicturesService.GetPosCoverOutput(picturePath, pictureBlurHash);
                return Ok(pos.ToOutput(picPosCover));
            }
            catch(Exception) {
                Logger.LogError("Failed to update POS {0}", posId);
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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteCover(
            [FromRoute] ObjectId posId
        ) {
            (_, var pos) = await VerifyUserIsAdminOfPos(posId);

            try {
                await PosService.UpdatePosCover(posId, null, null);
                await OfferService.UpdatePosCovers(posId, null, null);

                return Ok(pos.ToOutput(null));
            }
            catch(Exception) {
                Logger.LogError("Failed to update POS {0}", posId);
                throw;
            }
        }

        [HttpDelete("{posId}")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(PosDeleteOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(
            [FromRoute] ObjectId posId,
            [FromQuery] bool dryRun = false
        ) {
            Logger.LogInformation("Deleting POS {0} ({1})", posId, dryRun ? "dry run" : "effective run");

            (_, _) = await VerifyUserIsAdminOfPos(posId);

            var countOffers = await OfferService.CountActiveOffersOfPos(posId);
            if(dryRun) {
                return Ok(new PosDeleteOutput {
                    OperationPerformed = false,
                    CountOfDeletedPos = 1,
                    CountOfDeletedOffers = countOffers,
                });
            }

            var countDeletedOffers = await OfferService.DeleteOffersByPos(posId);
            await PosService.DeletePos(posId);

            return Ok(new PosDeleteOutput {
                OperationPerformed = true,
                CountOfDeletedPos = 1,
                CountOfDeletedOffers = countDeletedOffers,
            });
        }
        
    }
}
