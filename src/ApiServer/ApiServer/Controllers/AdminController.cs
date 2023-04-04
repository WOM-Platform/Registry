using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.OutputModels.Source;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/admin")]
    [OperationsTags("Administration")]
    [RequireHttpsInProd]
    public class AdminController : BaseRegistryController {

        public AdminController(
            IServiceProvider serviceProvider,
            ILogger<AdminController> logger)
        : base(serviceProvider, logger) {
        }

        [HttpGet("export/merchants")]
        [Authorize]
        public async Task<ActionResult> ExportMerchantList(
        ) {
            if(!await VerifyUserIsAdmin()) {
                return Forbid();
            }

            var merchants = await MerchantService.GetAllMerchantsWithUsers();

            var sb = new StringBuilder();
            sb.AppendLine("Merchant,Fiscal code,Address,ZIP code,City,Country,Website,Admin name,Admin surname,Admin email,");
            foreach(var merchant in merchants) {
                foreach(var admin in merchant.Administrators) {
                    sb.AppendFormat(@"""{0}"",""{1}"",""{2}"",""{3}"",""{4}"",""{5}"",""{6}"",",
                        merchant.Name,
                        merchant.FiscalCode,
                        merchant.Address,
                        merchant.ZipCode?.ToUpperInvariant(),
                        merchant.City,
                        merchant.Country,
                        merchant.WebsiteUrl
                    );
                    sb.AppendFormat(@"""{0}"",""{1}"",""{2}"",",
                        admin.Name,
                        admin.Surname,
                        admin.Email
                    );
                    sb.AppendLine();
                }
            }

            string csv = sb.ToString();
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm");
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"merchants-{today}.csv");
        }

        [HttpPost("generate/source")]
        [Obsolete]
        [Authorize]
        [ProducesResponseType(typeof(SourceDetailsOutput), StatusCodes.Status200OK)]
        public async Task<ActionResult> GenerateNewSource(
            [FromQuery] [Required] [StringLength(64, MinimumLength = 3)] string name,
            [FromQuery] [Url] string url,
            [FromQuery] double? latitude,
            [FromQuery] double? longitude
        ) {
            if(!await VerifyUserIsAdmin()) {
                return Forbid();
            }

            var keys = CryptoHelper.CreateKeyPair();

            var source = await SourceService.CreateNewSource(name, url, keys);

            return Ok(source.ToDetailsOutput());
        }

        [HttpPost("migrate/merchant-user-access")]
        [Authorize]
        public async Task<ActionResult> MigrateMerchantsToNewUserAccessRules() {
            if(!await VerifyUserIsAdmin()) {
                return Forbid();
            }

            await MerchantService.MigrateToNewUserAccessControl();

            return Ok();
        }

        [HttpPost("migrate/offer-payment-information")]
        [Authorize]
        public async Task<ActionResult> MigrateOfferPaymentInformation() {
            if(!await VerifyUserIsAdmin()) {
                return Forbid();
            }

            await OfferService.MigratePaymentInformationInOffers();

            return Ok();
        }

    }
}
