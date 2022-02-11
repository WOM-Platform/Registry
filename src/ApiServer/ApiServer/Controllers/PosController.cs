using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/pos")]
    [RequireHttps]
    [OperationsTags("Point of service")]
    public class PosController : BaseRegistryController {

        private readonly MongoDatabase _mongo;

        public PosController(
            MongoDatabase mongo,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<PosController> logger
        ) : base(configuration, crypto, keyManager, logger) {
            _mongo = mongo;
        }

        /// <summary>
        /// POS registration payload.
        /// </summary>
        public record PosRegisterInput(
            [Required]
            ObjectId OwnerMerchantId,
            [Required]
            [MinLength(4)]
            string Name,
            [Required]
            double Latitude,
            [Required]
            double Longitude,
            [Url]
            string Url
        );

        /// <summary>
        /// Registers a new POS to the service.
        /// </summary>
        /// <param name="input">POS registration payload.</param>
        [HttpPut]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(PosRegisterInput input) {
            if(!User.GetUserId(out var loggedUserId)) {
                return Forbid();
            }

            var owningMerchant = await _mongo.GetMerchantById(input.OwnerMerchantId);
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
                    IsDummy = false
                };
                await _mongo.CreatePos(pos);

                return CreatedAtAction(
                    nameof(GetInformation),
                    new {
                        id = pos.Id
                    },
                    pos.ToOutput()
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
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInformation(
            [FromRoute] ObjectId id
        ) {
            var pos = await _mongo.GetPosById(id);
            if(pos == null) {
                return NotFound();
            }

            var merchant = await _mongo.GetMerchantById(pos.MerchantId);
            if(merchant == null) {
                Logger.LogWarning("Owning merchant {0} for POS {1} does not exist", pos.MerchantId, pos.Id);
                return NotFound();
            }

            // Forbid if logged user is not in admin list OR POS user list
            if(!User.GetUserId(out var loggedUserId) ||
               !(
                   merchant.AdministratorIds.Contains(loggedUserId) || merchant.PosUserIds.Contains(loggedUserId)
               )) {
                return Forbid();
            }

            return Ok(pos.ToOutput());
        }

        /// <summary>
        /// POS update payload.
        /// </summary>
        public record PosUpdateInput(
            [MinLength(4)]
            string Name,
            double? Latitude,
            double? Longitude,
            [Url]
            string Url
        );

        /// <summary>
        /// Updates information about an existing merchant.
        /// </summary>
        /// <param name="id">Merchant ID.</param>
        /// <param name="input">Updated information.</param>
        [HttpPatch("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Update(
            [FromRoute] ObjectId id,
            PosUpdateInput input
        ) {
            var pos = await _mongo.GetPosById(id);
            if(pos == null) {
                return NotFound();
            }

            var merchant = await _mongo.GetMerchantById(pos.MerchantId);
            if(merchant == null) {
                Logger.LogWarning("Owning merchant {0} for POS {1} does not exist", pos.MerchantId, pos.Id);
                return NotFound();
            }

            // Forbid if logged user is not in admin list
            if(!User.GetUserId(out var loggedUserId) ||
               !merchant.AdministratorIds.Contains(loggedUserId)) {
                return Forbid();
            }

            try {
                if(input.Name != null) {
                    pos.Name = input.Name;
                }
                if(input.Latitude.HasValue && input.Longitude.HasValue) {
                    pos.Position = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                        new GeoJson2DGeographicCoordinates(
                            input.Longitude.Value,
                            input.Latitude.Value
                        )
                    );
                }
                if(input.Url != null) {
                    pos.Url = input.Url;
                }
                pos.LastUpdate = DateTime.UtcNow;

                await _mongo.ReplacePos(pos);
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to update POS {0}", id);
                throw;
            }

            return Ok(pos.ToOutput());
        }

    }

}
