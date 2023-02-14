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
using WomPlatform.Web.Api.OutputModels.Pos;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Methods used to provide authentication and key exchange for clients.
    /// </summary>
    [Route("v2/auth")]
    [OperationsTags("Authentication")]
    public class AuthControllerV2 : BaseRegistryController {

        public AuthControllerV2(
            IServiceProvider serviceProvider,
            ILogger<AdminController> logger)
        : base(serviceProvider, logger) {
        }

        public record AuthV2PosLoginOutput(
            string Name,
            string Surname,
            string Email,
            MerchantAuthOutput[] Merchants
        );

        /// <summary>
        /// Retrieves available WOM Merchants for the authenticated user.
        /// </summary>
        [HttpPost("merchant")]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        [RequireHttpsInProd]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AuthV2PosLoginOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PosLoginV2() {
            Logger.LogDebug("POS login V2");

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

            return Ok(new AuthV2PosLoginOutput(
                userData.Name,
                userData.Surname,
                userData.Email,
                data.Select(d => new MerchantAuthOutput {
                    Id = d.Item1.Id.ToString(),
                    Name = d.Item1.Name,
                    FiscalCode = d.Item1.FiscalCode,
                    PrimaryActivity = d.Item1.PrimaryActivityType,
                    Address = d.Item1.Address,
                    ZipCode = d.Item1.ZipCode,
                    City = d.Item1.City,
                    Country = d.Item1.Country,
                    Description = d.Item1.Description,
                    Url = d.Item1.WebsiteUrl,
                    Pos = (from p in d.Item2
                           let pictureOutput = PicturesService.GetPictureOutput(p.CoverPath, p.CoverBlurHash)
                           select p.ToAuthOutput(pictureOutput)).ToArray(),
                    Enabled = d.Item1.Enabled
                }).ToArray()
            ));
        }

        public record AuthV2SourceLoginOutput(
            string Name,
            string Surname,
            string Email,
            SourceLoginV2Output[] Sources
        );

        /// <summary>
        /// Retrieves available WOM Merchants for the authenticated user.
        /// </summary>
        [HttpPost("source")]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        [RequireHttpsInProd]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AuthV2SourceLoginOutput), StatusCodes.Status200OK)]
        public async Task<IActionResult> SourceLoginV2() {
            Logger.LogDebug("Source login V2");

            if(!User.GetUserId(out var userId)) {
                return Forbid();
            }

            var user = await UserService.GetUserById(userId);
            var sources = await SourceService.GetSourcesByUser(userId);
            Logger.LogInformation("User {0} controls {1} sources", userId, sources.Count);

            var allAims = (from a in await AimService.GetRootAims() select a.Code).ToArray();

            return Ok(new AuthV2SourceLoginOutput(
                user.Name,
                user.Surname,
                user.Email,
                sources.Select(s => s.ToLoginV2Output(allAims)).ToArray()
            ));
        }

        public record AuthV2CreateSourceApiKeyOutput(
            string sourceId,
            string selector,
            ApiKey.KindOfKey kind,
            string apiKey
        );

        /// <summary>
        /// Creates a new API key access for a given source.
        /// </summary>
        [HttpPost("source/{sourceId}/apikey")]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        [RequireHttpsInProd]
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

        /// <summary>
        /// Retrieves the public key used by the WOM Registry.
        /// </summary>
        [HttpGet("key")]
        [RequireHttpsInProd]
        [Produces("text/plain")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public IActionResult GetPublicKey() {
            return Ok(KeyManager.RegistryPublicKey.ToPemString());
        }

        public record GetAnonymousCredentialsOutput(
            string PosId,
            string posPrivateKey
        );

        /// <summary>
        /// Retrieves the auth information (ID and private key) used by the anonymous POS.
        /// </summary>
        [HttpGet("anonymous")]
        [RequireHttpsInProd]
        [Produces("application/json")]
        [ProducesResponseType(typeof(GetAnonymousCredentialsOutput), StatusCodes.Status200OK)]
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

            return Ok(new GetAnonymousCredentialsOutput(posId, pos.PrivateKey));
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
        [RequireHttpsInProd]
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
            Location DefaultLocation,
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
                    (source.Aims.EnableAll ? allAims : source.Aims.Enabled).ToSafeArray(),
                    (source.Location.Position == null) ? null : new Location {
                        Latitude = source.Location.Position.Coordinates.Latitude,
                        Longitude = source.Location.Position.Coordinates.Longitude
                    },
                    source.Location.IsFixed
                )
            ));
        }

    }

}
