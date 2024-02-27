using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;
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
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(GenerationRequestOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GenerateVouchers(
            [FromRoute] ObjectId sourceId,
            [FromBody] GenerateVouchersInput input
        ) {
            if(!Request.Headers.TryGetValue("X-WOM-ApiKey", out var apiKeyHeader)) {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Request does not contain X-WOM-ApiKey header");
            }

            var apiKey = apiKeyHeader.ToString();
            var entry = await ApiKeyService.RetrieveApiKey(apiKey);
            if(entry == null || entry.Expired) {
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: "API key not valid");
            }
            if(entry.Kind != ApiKey.KindOfKey.SourceAdministrator) {
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: "API key does not control source");
            }
            if(entry.ControlledEntityId != sourceId) {
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: $"API key does not allow control of source {sourceId}");
            }

            var source = await SourceService.GetSourceById(entry.ControlledEntityId);
            // TODO: other checks?

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
