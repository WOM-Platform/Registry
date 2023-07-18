using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

        [HttpPost("generate")]
        [Authorize]
        public async Task<ActionResult> GenerateBatchVouchers(
            [FromBody] VoucherBatchGenerationInput input
        ) {
            Logger.LogInformation("Voucher batch generation for {0} users on behalf of {1}", input.Users.Length, input.SourceId);

            var source = await VerifyUserIsAdminOfSource(input.SourceId);

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
