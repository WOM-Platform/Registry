using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.OutputModels.Source;
using WomPlatform.Web.Api.OutputModels.User;
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
            await VerifyUserIsAdmin();

            var merchants = await MerchantService.GetAllMerchantsAndUsers();

            var sb = new StringBuilder();
            sb.AppendLine("Merchant,Fiscal code,Address,ZIP code,City,Country,Website,Admin name,Admin surname,Admin email,");
            foreach(var merchant in merchants) {
                foreach(var admin in merchant.Administrators) {
                    sb.AppendFormat(@"""{0}"",""{1}"",""{2}"",""{3}"",""{4}"",""{5}"",""{6}"",",
                        merchant.Name,
                        merchant.FiscalCode,
                        (merchant.Address?.StreetName).OptConcat(merchant.Address?.StreetNumber),
                        merchant.Address?.ZipCode.ToUpperInvariant(),
                        merchant.Address?.City,
                        merchant.Address?.Country,
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

        [HttpPost("generate/user")]
        [Authorize]
        [ProducesResponseType(typeof(UserCreationOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> GenerateNewUser(
            [FromQuery] [Required] [EmailAddress] string email,
            [FromQuery] [Required] string name,
            [FromQuery] [Required] string surname,
            [FromQuery] string password,
            [FromQuery] ObjectId? adminOfSource,
            [FromQuery] ObjectId? adminOfMerchant
        ) {
            await VerifyUserIsAdmin();

            string generatedPassword = Crypto.Generator.GenerateCode(16);
            if(password != null && !CheckUserPassword(password)) {
                return Problem(title: "Password unacceptable", statusCode: StatusCodes.Status400BadRequest);
            }

            var session = await CreateMongoSession();
            var user = await session.WithTransactionAsync(async (session, token) => {
                var user = await UserService.CreateUser(session, email, name, surname, password ?? generatedPassword, isVerified: true);
                Logger.LogInformation("New user {0} created for {1}", user.Id, user.Email);

                if(adminOfSource.HasValue) {
                    await SourceService.AddUserAsAdministrator(session, adminOfSource.Value, user);
                }

                if(adminOfMerchant.HasValue) {
                    await MerchantService.AddUserAsManager(session,adminOfMerchant.Value, user, MerchantRole.Admin);
                }

                return user;
            });

            return Ok(new UserCreationOutput {
                Id = user.Id,
                Email = user.Email,
                GeneratedPassword = (password == null) ? generatedPassword : null,
                Name = user.Name,
                Surname = user.Surname,
                Verified = user.VerificationToken == null,
            });
        }

        [HttpPut("users/password")]
        [Authorize]
        public async Task<ActionResult> ResetUserPassword(
            [FromQuery] [Required] [EmailAddress] string email,
            [FromQuery] [Required] string password
        ) {
            await VerifyUserIsAdmin();

            var user = await UserService.GetUserByEmail(email);
            if(user == null) {
                return NotFound();
            }

            await UserService.PerformPasswordReset(user.Id, null, password, UserService.TokenVerification.Skip);

            return Ok(user.ToOutput(false));
        }

        [HttpPost("migrate/offer-payment-information")]
        [Authorize]
        public async Task<ActionResult> MigrateOfferPaymentInformation() {
            await VerifyUserIsAdmin();

            await OfferService.MigratePaymentInformationInOffers();

            return Ok();
        }

    }
}
