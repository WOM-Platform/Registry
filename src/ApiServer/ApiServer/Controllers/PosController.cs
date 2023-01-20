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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels.Pos;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Pos;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/pos")]
    [RequireHttpsInProd]
    [OperationsTags("Point of service")]
    public class PosController : BaseRegistryController {

        private readonly MerchantService _merchantService;
        private readonly PosService _posService;
        private readonly OfferService _offerService;
        private readonly PicturesService _picturesService;

        public PosController(
            MerchantService merchantService,
            PosService posService,
            OfferService offerService,
            PicturesService picturesService,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<PosController> logger
        ) : base(configuration, crypto, keyManager, logger) {
            _merchantService = merchantService;
            _posService = posService;
            _offerService = offerService;
            _picturesService = picturesService;
        }

        /// <summary>
        /// Registers a new POS to the service.
        /// </summary>
        /// <param name="input">POS registration payload.</param>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(PosOutput), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(PosRegistrationInput input) {
            if(!User.GetUserId(out var loggedUserId)) {
                return Forbid();
            }

            var owningMerchant = await _merchantService.GetMerchantById(input.OwnerMerchantId);
            if(owningMerchant == null) {
                return Problem("Owning merchant does not exist");
            }

            if(!owningMerchant.AdministratorIds.Contains(loggedUserId)) {
                return Forbid();
            }

            try {
                var posKeys = CryptoHelper.CreateKeyPair();

                var pos = new Pos {
                    MerchantId = owningMerchant.Id,
                    Name = input.Name,
                    Position = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                        new GeoJson2DGeographicCoordinates(
                            input.Longitude,
                            input.Latitude
                        )
                    ),
                    Url = input.Url,
                    PrivateKey = posKeys.Private.ToPemString(),
                    PublicKey = posKeys.Public.ToPemString(),
                    CreatedOn = DateTime.UtcNow,
                    IsDummy = false,
                    IsActive = true,
                };
                await _posService.CreatePos(pos);

                return CreatedAtAction(
                    nameof(GetInformation),
                    new {
                        id = pos.Id
                    },
                    pos.ToOutput(null)
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
        /// <param name="id">POS ID.</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PosOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInformation(
            [FromRoute] ObjectId id
        ) {
            var pos = await _posService.GetPosById(id);
            if(pos == null) {
                return NotFound();
            }

            var picCover = _picturesService.GetPictureOutput(pos.CoverPath, pos.CoverBlurHash);

            return Ok(pos.ToOutput(picCover));
        }

        public record PosListOutput(
            PosOutput[] Pos,
            int Page,
            int PageSize,
            bool HasPrevious,
            bool HasNext,
            long TotalCount
        );

        [HttpGet]
        [ProducesResponseType(typeof(PosListOutput), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(
            [FromQuery] double? latitude,
            [FromQuery] double? longitude,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] [DefaultValue(PosService.PosListOrder.Name)] PosService.PosListOrder orderBy = PosService.PosListOrder.Name
        ) {
            GeoCoords? near = (latitude.HasValue && longitude.HasValue) ?
                new GeoCoords { Latitude = latitude.Value, Longitude = longitude.Value } :
                null;

            (var results, var count) = await _posService.ListPos(near, page, pageSize, orderBy);

            return Ok(Paged<PosOutput>.FromPage(
                (from pos in results
                 let picCover = _picturesService.GetPictureOutput(pos.CoverPath, pos.CoverBlurHash)
                 select pos.ToOutput(picCover)).ToArray(),
                page,
                pageSize,
                count
            ));
        }

        /// <summary>
        /// Updates information about an existing POS.
        /// </summary>
        /// <param name="id">Merchant ID.</param>
        /// <param name="input">Updated information.</param>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(PosOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            [FromRoute] ObjectId id,
            [FromBody] PosUpdateInput input
        ) {
            var pos = await _posService.GetPosById(id);
            if(pos == null) {
                return NotFound();
            }

            var merchant = await _merchantService.GetMerchantById(pos.MerchantId);
            if(merchant == null) {
                Logger.LogError("Owning merchant {0} for POS {1} does not exist", pos.MerchantId, pos.Id);
                return NotFound();
            }

            // Forbid if logged user is not in admin list
            if(!User.GetUserId(out var loggedUserId) || !merchant.AdministratorIds.Contains(loggedUserId)) {
                return Forbid();
            }

            try {
                pos.Name = input.Name;
                pos.Description = input.Description;
                pos.Position = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(
                        input.Longitude,
                        input.Latitude
                    )
                );
                pos.Url = input.Url;
                pos.IsActive = input.IsActive;
                pos.LastUpdate = DateTime.UtcNow;

                await _posService.ReplacePos(pos);
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to update POS {0}", id);
                throw;
            }

            var picPosCover = _picturesService.GetPictureOutput(pos.CoverPath, pos.CoverBlurHash);

            return Ok(pos.ToOutput(picPosCover));
        }

        /// <summary>
        /// Updates the cover of an existing POS.
        /// </summary>
        [HttpPost("{id}/cover")]
        [Authorize]
        [DisableRequestSizeLimit]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(PosOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCover(
            [FromRoute] ObjectId id,
            [FromForm] [Required] IFormFile image
        ) {
            var pos = await _posService.GetPosById(id);
            if(pos == null) {
                return NotFound();
            }

            var merchant = await _merchantService.GetMerchantById(pos.MerchantId);
            if(merchant == null) {
                Logger.LogError("Owning merchant {0} for POS {1} does not exist", pos.MerchantId, pos.Id);
                return NotFound();
            }

            // Forbid if logged user is not in admin list
            if(!User.GetUserId(out var loggedUserId) || !merchant.AdministratorIds.Contains(loggedUserId)) {
                return Forbid();
            }

            // Safety checks on uploaded file
            if(image == null || image.Length == 0) {
                return BadRequest();
            }
            if(image.Length > 4 * 1024 * 1024) {
                return BadRequest();
            }

            try {
                var posUrl = string.Format("{0}-{1}", merchant.City, pos.Name).ToCleanUrl();

                // Process and upload image
                using var stream = new MemoryStream();
                await image.CopyToAsync(stream);
                (var picturePath, var pictureBlurHash) = await _picturesService.ProcessAndUploadPicture(stream, posUrl, PicturesService.PictureUsage.PosCover);

                await _posService.UpdatePosCover(id, picturePath, pictureBlurHash);
                await _offerService.UpdatePosCovers(id, picturePath, pictureBlurHash);

                var picPosCover = _picturesService.GetPictureOutput(picturePath, pictureBlurHash);
                return Ok(pos.ToOutput(picPosCover));
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to update POS {0}", id);
                throw;
            }
        }

    }

}
