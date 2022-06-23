using System;
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
using WomPlatform.Connector;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/source")]
    [RequireHttps]
    [OperationsTags("Instrument")]
    public class SourceController : BaseRegistryController 
    {
        private readonly MongoDatabase _mongo;
        private readonly SourceService _sourceService;

        public SourceController(
            MongoDatabase mongo,
            SourceService sourceService,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<SourceController> logger
        ) : base(configuration, crypto, keyManager, logger) {
            _mongo = mongo;
            _sourceService = sourceService;
        }

        /// <summary>
        /// Provides a count of vouchers produced by a given source.
        /// Request must be authorized by a user who is an administrator of the source.
        /// </summary>
        [HttpGet("generated/{sourceId}")]
        [Authorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(SourceGeneratedCountOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSourceGeneratedVouchersCount([FromRoute] ObjectId sourceId)
        {
            var source = await _mongo.GetSourceById(sourceId);
            if(source == null) {
                return NotFound();
            }

            if(!User.GetUserId(out var loggedUserId) || !source.AdministratorUserIds.Contains(loggedUserId)) {
                return Forbid();
            }
            
            var result = await _sourceService.GetGeneratedVouchersBySource(sourceId);
            return Ok(new SourceGeneratedCountOutput {
                Total = (int)result?.Total
            });
        }
    }    
}
