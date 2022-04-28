using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/admin")]
    [OperationsTags("Administration")]
    public class AdminController : BaseRegistryController {

        private readonly MerchantService _merchantService;

        public AdminController(
            MerchantService merchantService,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<AdminController> logger)
        : base(configuration, crypto, keyManager, logger) {
            _merchantService = merchantService;
        }

        [HttpGet("export/merchants")]
        [AllowAnonymous]
        public async Task<IActionResult> ExportMerchantList(
        ) {
            var merchants = await _merchantService.GetAllMerchantsWithUsers();

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

    }
}
