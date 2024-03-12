using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels.Generation;
using WomPlatform.Web.Api.Mail;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/voucher/batch")]
    [OperationsTags("Voucher generation")]
    [RequireHttpsInProd]
    public class VoucherBatchController : BaseRegistryController {

        private readonly MailerComposer _composer;

        public VoucherBatchController(
            MailerComposer composer,
            IServiceProvider serviceProvider,
            ILogger<VoucherBatchController> logger)
        : base(serviceProvider, logger) {
            _composer = composer;
        }

        /// <summary>
        /// Generate batches of vouchers for users on behalf of a source.
        /// </summary>
        /// <remarks>
        /// The user must be logged-in using token Bearer authentication or an API key must be supplied as the "X-WOM-ApiKey" HTTP header.
        /// </remarks>
        [HttpPost("generate")]
        public async Task<ActionResult> GenerateBatchVouchers(
            [FromBody] VoucherBatchGenerationInput input
        ) {
            if(Request.Headers.TryGetValue("X-WOM-ApiKey", out var apiKeyHeader)) {
                var apiKey = apiKeyHeader.ToString();
                var entry = await ApiKeyService.RetrieveApiKey(apiKey);
                if(entry == null || entry.Expired) {
                    return Problem(statusCode: StatusCodes.Status403Forbidden, title: "API key not valid");
                }

                if(entry.Kind != ApiKey.KindOfKey.SourceAdministrator) {
                    return Problem(statusCode: StatusCodes.Status403Forbidden, title: "API key does not grant access to source");
                }
            }
            else {
                // User must be logged-in
                await VerifyUserIsAdminOfSource(input.SourceId);
            }

            Logger.LogInformation("Voucher batch generation for {0} users on behalf of {1}", input.Users.Length, input.SourceId);

            var source = await SourceService.GetSourceById(input.SourceId);

            foreach(var user in input.Users) {
                (var generation, _) = await GenerationService.CreateGenerationRequest(source, new VoucherGenerationSpecification[] {
                    new VoucherGenerationSpecification {
                        Count = user.Count,
                        Aim = user.Aim,
                        Location = user.Location,
                        Timestamp = user.Timestamp,
                        CreationMode = user.CreationMode,
                    }
                }, isPreVerified: true);

                _composer.SendVouchers(user.Email, input.Title, source.Name, $"https://{SelfLinkDomain}/vouchers/{generation.Otc:D}", generation.Password);
            }

            return Ok();
        }

    }

}
