using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Connector;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Methods used to provide authentication and key exchange for clients.
    /// </summary>
    [Route("v2/auth")]
    [OperationsTags("Authentication")]
    public class AuthControllerV2 : BaseRegistryController {

        private readonly MongoDatabase _mongo;

        public AuthControllerV2(
            MongoDatabase mongo,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<AuthControllerV2> logger)
        : base(configuration, crypto, keyManager, logger) {
            _mongo = mongo;
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
        [RequireHttps]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AuthV2PosLoginOutput), StatusCodes.Status200OK)]
        public async Task<IActionResult> PosLoginV2() {
            Logger.LogDebug("POS login V2");

            if(!User.GetUserId(out var userId)) {
                return Forbid();
            }

            var userData = await _mongo.GetUserById(userId);

            var data = await _mongo.GetMerchantsAndPosByUser(userId);
            Logger.LogInformation("User {0} controls POS for {1} merchants", userId, data.Count);

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
                    Pos = d.Item2.Select(p => p.ToLoginOutput()).ToArray()
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
        [RequireHttps]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AuthV2SourceLoginOutput), StatusCodes.Status200OK)]
        public async Task<IActionResult> SourceLoginV2() {
            Logger.LogDebug("Source login V2");

            if(!User.GetUserId(out var userId)) {
                return Forbid();
            }

            var user = await _mongo.GetUserById(userId);
            var sources = await _mongo.GetSourcesByUser(userId);
            Logger.LogInformation("User {0} controls {1} sources", userId, sources.Count);

            var allAims = (from a in await _mongo.GetRootAims() select a.Code).ToList();

            return Ok(new AuthV2SourceLoginOutput(
                user.Name,
                user.Surname,
                user.Email,
                sources.Select(s => new SourceLoginV2Output {
                    Id = s.Id.ToString(),
                    Name = s.Name,
                    Url = s.Url,
                    PrivateKey = s.PrivateKey,
                    PublicKey = s.PublicKey,
                    EnabledAims = s.Aims.EnableAll ? allAims : s.Aims.Enabled.ToSafeList(),
                    PerAimBudget = s.Aims.CurrentBudget ?? new Dictionary<string, int>(),
                    DefaultLocation = (s.Location.Position == null) ? null : new Location {
                        Latitude = s.Location.Position.Coordinates.Latitude,
                        Longitude = s.Location.Position.Coordinates.Longitude
                    },
                    LocationIsFixed = s.Location.IsFixed
                }).ToArray()
            ));
        }

        /// <summary>
        /// Retrieves the public key used by the WOM Registry.
        /// </summary>
        [HttpGet("key")]
        [RequireHttps]
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
        [RequireHttps]
        [Produces("application/json")]
        [ProducesResponseType(typeof(GetAnonymousCredentialsOutput), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAnonymousCredentials() {
            var anonymousSection = Configuration.GetSection("AnonymousSetup");
            var posId = anonymousSection["Id"];

            if(!ObjectId.TryParse(posId, out var id)) {
                return NotFound();
            }

            var pos = await _mongo.GetPosById(id);
            if(pos == null) {
                return NotFound();
            }

            return Ok(new GetAnonymousCredentialsOutput(posId, pos.PrivateKey));
        }

    }

}
