﻿using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.OutputModels.Map;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [ApiVersionNeutral]
    [Route("v1/map")]
    public class MapController : BaseRegistryController {

        private readonly MapService _mapService;

        public MapController(
            MapService mapService,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, logger) {
            _mapService = mapService;
        }

        /// <summary>
        /// Provides a list of POS within a bounding box.
        /// </summary>
        /// <param name="llx">Lower-left longitude.</param>
        /// <param name="lly">Lower-left latitude.</param>
        /// <param name="urx">Upper-right longitude.</param>
        /// <param name="ury">Upper-right latitude.</param>
        [HttpPost("pos")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(PosBoxResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVoucherStats(
            [FromQuery] double llx, [FromQuery] double lly,
            [FromQuery] double urx, [FromQuery] double ury
        ) {
            var results = await _mapService.FetchPosWithin(llx, lly, urx, ury);

            return Ok(new PosBoxResponse {
                Pos = (from r in results
                       select new PosBoxResponse.PosEntry {
                           Id = r.Id.ToString(),
                           Name = r.Name,
                           Position = new OutputModels.GeoCoords {
                               Longitude = r.Position.Coordinates.Longitude,
                               Latitude = r.Position.Coordinates.Latitude
                           },
                           Url = r.Url
                       }).ToArray(),
                LowerLeft = new OutputModels.GeoCoords { Longitude = llx, Latitude = lly },
                UpperRight = new OutputModels.GeoCoords { Longitude = urx, Latitude = ury }
            });
        }

    }
}
