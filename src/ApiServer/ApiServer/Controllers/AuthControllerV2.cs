using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Authentication;
using WomPlatform.Web.Api.OutputModels.Merchant;
using WomPlatform.Web.Api.OutputModels.Pos;
using WomPlatform.Web.Api.OutputModels.Source;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Methods used to provide authentication and key exchange for clients.
    /// </summary>
    [Route("v2/auth")]
    [OperationsTags("Authentication")]
    [RequireHttpsInProd]
    public class AuthControllerV2 : BaseRegistryController {

        public AuthControllerV2(
            IServiceProvider serviceProvider,
            ILogger<AuthControllerV2> logger)
        : base(serviceProvider, logger) {
        }

        /// <summary>
        /// Retrieves available WOM Merchants for the authenticated user.
        /// </summary>
        [HttpPost("merchant")]
        [Obsolete]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        [Produces("application/json")]
        [ProducesResponseType(typeof(MerchantDashboardOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> MerchantLoginV2() {
            Logger.LogDebug("Merchant login V2");

            if(!User.GetUserId(out var userId)) {
                return Forbid();
            }

            var userData = await UserService.GetUserById(userId);
            if(userData == null) {
                Logger.LogError("Logged in user {0} does not have a user profile", userId);
                return Forbid();
            }

            var data = await PosService.GetMerchantsAndPosByUser(userId);
            Logger.LogInformation("User {0} authenticated with access to {1} merchants", userId, data.Count);

            return Ok(new MerchantDashboardOutput {
                Name = userData.Name,
                Surname = userData.Surname,
                Email = userData.Email,
                Merchants = (from m in data.Keys
                             let pos = data[m]
                             orderby m.Id
                             select m.ToAuthOutput(
                                 (from p in pos
                                  let pictureOutput = PicturesService.GetPosCoverOutput(p.CoverPath, p.CoverBlurHash)
                                  select p.ToAuthOutput(pictureOutput)).ToArray(),
                                 m.Access.Get(userId).Role
                             )).ToArray()
            });
        }

        /// <summary>
        /// Retrieves available WOM Merchants for the authenticated user.
        /// </summary>
        [HttpPost("source")]
        [Obsolete]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        [Produces("application/json")]
        [ProducesResponseType(typeof(SourceDashboardOutput), StatusCodes.Status200OK)]
        public async Task<IActionResult> SourceLoginV2() {
            Logger.LogDebug("Source login V2");

            if(!User.GetUserId(out var userId)) {
                return Forbid();
            }

            var user = await UserService.GetUserById(userId);
            var sources = await SourceService.GetSourcesByUser(userId);
            Logger.LogInformation("User {0} controls {1} sources", userId, sources.Count);

            var allAims = (from a in await AimService.GetRootAims() select a.Code).ToArray();

            return Ok(new SourceDashboardOutput {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                Sources = (from s in sources
                           let customGeneratorPic = PicturesService.GetPictureOutput(s.CustomGenerator?.LogoPath, s.CustomGenerator?.LogoBlurHash)
                           select new SourceAuthDetailsOutput(s, allAims, customGeneratorPic)).ToArray(),
            });
        }

        public record AuthV2CreateSourceApiKeyOutput(
            string SourceId,
            string Selector,
            ApiKey.KindOfKey Kind,
            string ApiKey
        );

        /// <summary>
        /// Creates a new API key access for a given source.
        /// </summary>
        [HttpPost("source/{sourceId}/apikey")]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AuthV2CreateSourceApiKeyOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateSourceApiKey(
            [FromRoute] ObjectId sourceId,
            [FromQuery] [Required] string selector,
            [FromQuery] ApiKey.KindOfKey kind = ApiKey.KindOfKey.SourceAdministrator
        ) {
            Logger.LogDebug("Create source API key");

            if(!User.GetUserId(out var userId)) {
                return Forbid();
            }

            var source = await SourceService.GetSourceById(sourceId);
            if(source == null) {
                return NotFound();
            }
            if(!source.AdministratorUserIds.Contains(userId)) {
                return Forbid();
            }

            if(string.IsNullOrWhiteSpace(selector)) {
                return Problem(statusCode: 400, title: "API key selector cannot be null or empty");
            }
            if(!Enum.IsDefined(kind)) {
                return Problem(statusCode: 400, title: "API key kind is not valid");
            }

            var apiKey = await ApiKeyService.CreateOrGetApiKey(sourceId, selector, kind);

            return Ok(new AuthV2CreateSourceApiKeyOutput(
                sourceId.ToString(),
                selector,
                kind,
                apiKey.Key
            ));
        }

        public record GetApiKeyCredentialsOutput(
            string EntityKind,
            string EntityId,
            string PrivateKey,
            string PublicKey,
            object Details
        );

        /// <summary>
        /// Retrieves credentials bound to an API key.
        /// </summary>
        /// <remarks>
        /// The API key must be supplied as the "X-WOM-ApiKey" HTTP header.
        /// </remarks>
        [HttpPost("apikey")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(GetApiKeyCredentialsOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetApiKeyCredentials() {
            if(!Request.Headers.TryGetValue("X-WOM-ApiKey", out var apiKeyHeader)) {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Request does not contain X-WOM-ApiKey header");
            }

            var apiKey = apiKeyHeader.ToString();
            var entry = await ApiKeyService.RetrieveApiKey(apiKey);
            if(entry == null || entry.Expired) {
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: "API key not valid");
            }

            return entry.Kind switch {
                ApiKey.KindOfKey.SourceAdministrator => await GetApiKeyCredentialsSource(entry, entry.ControlledEntityId),
                _ => Problem(statusCode: StatusCodes.Status400BadRequest, title: "API key does not match a recognized entity kind"),
            };
        }

        private record GetApiKeySourceDetails(
            string Name,
            string Url,
            string[] EnabledAims,
            GeoCoordsOutput DefaultLocation,
            bool LocationIsFixed
        );

        private async Task<IActionResult> GetApiKeyCredentialsSource(ApiKey apiKey, ObjectId sourceId) {
            var source = await SourceService.GetSourceById(sourceId);
            if(source == null) {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "Source bound to API key does not exist");
            }

            var allAims = (from a in await AimService.GetRootAims() select a.Code).ToArray();

            return Ok(new GetApiKeyCredentialsOutput(
                "Source", sourceId.ToString(), apiKey.PrivateKey, apiKey.PublicKey,
                new GetApiKeySourceDetails(
                    source.Name,
                    source.Url,
                    (source.Aims?.EnableAll ?? false ? allAims : source.Aims?.Enabled).ToSafeArray(),
                    (source.Location?.Position == null) ? null : new GeoCoordsOutput {
                        Latitude = source.Location.Position.Coordinates.Latitude,
                        Longitude = source.Location.Position.Coordinates.Longitude
                    },
                    source.Location?.IsFixed ?? false
                )
            ));
        }

        /// <summary>
        /// Retrieves the public key used by the WOM Registry.
        /// </summary>
        [HttpGet("key")]
        [Produces("text/plain")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public IActionResult GetPublicKey() {
            return Ok(KeyManager.RegistryPublicKey.ToPemString());
        }

        public record GetPosCredentialsOutput(
            string PosId,
            string PosPrivateKey
        );

        /// <summary>
        /// Retrieves the auth information (ID and private key) used by the anonymous POS.
        /// </summary>
        [HttpGet("anonymous")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(GetPosCredentialsOutput), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAnonymousCredentials() {
            var anonymousSection = Configuration.GetSection("AnonymousSetup");
            var posId = anonymousSection["Id"];

            if(!ObjectId.TryParse(posId, out var id)) {
                return NotFound();
            }

            var pos = await PosService.GetPosById(id);
            if(pos == null) {
                return NotFound();
            }

            return Ok(new GetPosCredentialsOutput(posId, pos.PrivateKey));
        }

        public record GetSourceCredentialsOutput(
            string SourceId,
            string SourcePrivateKey
        );

        /// <summary>
        /// Retrieves the auth information (ID and private key) used for the voucher transfer protocol.
        /// </summary>
        [HttpGet("exchange")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(GetSourceCredentialsOutput), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExchangeCredentials() {
            var anonymousSection = Configuration.GetSection("AnonymousSetup");
            var posId = anonymousSection["Id"];

            if(!ObjectId.TryParse(posId, out var id)) {
                return NotFound();
            }

            var pos = await PosService.GetPosById(id);
            if(pos == null) {
                return NotFound();
            }

            return Ok(new GetSourceCredentialsOutput(posId, pos.PrivateKey));
        }

    }

}
