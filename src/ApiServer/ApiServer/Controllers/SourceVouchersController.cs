using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.Authentication;
using WomPlatform.Web.Api.InputModels.Generation;
using WomPlatform.Web.Api.OutputModels.Vouchers;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/source/{sourceId}/vouchers")]
    [OperationsTags("Instrument")]
    [RequireHttpsInProd]
    public class SourceVouchersController : BaseRegistryController {
        public SourceVouchersController(
            IServiceProvider serviceProvider,
            ILogger<SourceVouchersController> logger)
        : base(serviceProvider, logger) {
        }

        public record GenerateVouchersInput(VoucherGenerationSpecification[] Vouchers);

        [HttpPost]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationSchemeOptions.SchemeName)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(GenerationRequestOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GenerateVouchers(
            [FromRoute] ObjectId sourceId,
            [FromBody] GenerateVouchersInput input
        ) {
            if(!User.GetSourceId(out var controlledSourceId)) {
                return Unauthorized();
            }
            if(sourceId != controlledSourceId) {
                return Forbid();
            }

            var source = await SourceService.GetSourceById(sourceId);

            (var generationRequest, _) = await GenerationService.CreateGenerationRequest(source, input.Vouchers, isPreVerified: true);

            return Ok(new GenerationRequestOutput {
                RegistryUrl = $"https://{SelfHostDomain}",
                Nonce = generationRequest.Nonce,
                Otc = generationRequest.Otc,
                Password = generationRequest.Password,
                Link = $"https://{SelfLinkDomain}/vouchers/{generationRequest.Otc:D}",
                Count = generationRequest.TotalVoucherCount.Value,
            });
        }
    }
}
