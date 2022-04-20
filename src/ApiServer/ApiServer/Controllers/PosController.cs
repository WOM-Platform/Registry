using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private readonly MerchantService _merchantService;
        private readonly PosService _posService;

        public PosController(
            MongoDatabase mongo,
            MerchantService merchantService,
            PosService posService,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<PosController> logger
        ) : base(configuration, crypto, keyManager, logger) {
            _mongo = mongo;
            _merchantService = merchantService;
            _posService = posService;
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
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(PosOutput), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(PosRegisterInput input) {
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
        [ProducesResponseType(typeof(PosOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInformation(
            [FromRoute] ObjectId id
        ) {
            var pos = await _posService.GetPosById(id);
            if(pos == null) {
                return NotFound();
            }

            return Ok(pos.ToOutput());
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
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 10,
            [FromQuery] [DefaultValue(PosService.PosListOrder.Name)] PosService.PosListOrder orderBy = PosService.PosListOrder.Name
        ) {
            GeoCoords? near = (latitude.HasValue && longitude.HasValue) ?
                new GeoCoords { Latitude = latitude.Value, Longitude = longitude.Value } :
                null;

            (var results, var count) = await _posService.ListPos(near, page, pageSize, orderBy);

            return Ok(new PosListOutput(
                (from pos in results
                 select pos.ToOutput()).ToArray(),
                page,
                pageSize,
                page > 1,
                count > (page * pageSize),
                count
            ));
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
            string Url,
            bool? IsActive
        );

        /// <summary>
        /// Updates information about an existing merchant.
        /// </summary>
        /// <param name="id">Merchant ID.</param>
        /// <param name="input">Updated information.</param>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Update(
            [FromRoute] ObjectId id,
            PosUpdateInput input
        ) {
            var pos = await _posService.GetPosById(id);
            if(pos == null) {
                return NotFound();
            }

            var merchant = await _merchantService.GetMerchantById(pos.MerchantId);
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
                if(input.IsActive.HasValue) {
                    pos.IsActive = input.IsActive.Value;
                }
                pos.LastUpdate = DateTime.UtcNow;

                await _posService.ReplacePos(pos);
            }
            catch(Exception ex) {
                Logger.LogError(ex, "Failed to update POS {0}", id);
                throw;
            }

            return Ok(pos.ToOutput());
        }

    }

}
