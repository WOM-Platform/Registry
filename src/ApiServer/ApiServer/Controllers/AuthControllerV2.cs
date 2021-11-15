using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.OutputModels;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Methods used to provide authentication and key exchange for clients.
    /// </summary>
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/auth")]
    [OperationsTags("Authentication")]
    public class AuthControllerV2 : BaseRegistryController {

        public AuthControllerV2(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<AuthControllerV2> logger)
        : base(configuration, crypto, keyManager, mongo, @operator, logger) {
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
        [Produces("application/json")]
        [RequireHttps]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        public async Task<IActionResult> PosLoginV2() {
            Logger.LogDebug("POS login V2");

            if(!User.GetUserId(out var userId)) {
                return Forbid();
            }

            var userData = await Mongo.GetUserById(userId);

            var data = await Mongo.GetMerchantsAndPosByUser(userId);
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
                    Pos = d.Item2.Select(p => new PosLoginOutput {
                        Id = p.Id.ToString(),
                        Name = p.Name,
                        Url = p.Url,
                        PrivateKey = p.PrivateKey,
                        PublicKey = p.PublicKey
                    }).ToArray()
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
        [Produces("application/json")]
        [RequireHttps]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        public async Task<IActionResult> SourceLoginV2() {
            Logger.LogDebug("Source login V2");

            if(!User.GetUserId(out var userId)) {
                return Forbid();
            }

            var user = await Mongo.GetUserById(userId);
            var sources = await Mongo.GetSourcesByUser(userId);
            Logger.LogInformation("User {0} controls {1} sources", userId, sources.Count);

            var allAims = (from a in await Mongo.GetRootAims() select a.Code).ToList();

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
        [Produces("text/plain")]
        public IActionResult GetPublicKey() {
            return Ok(KeyManager.RegistryPublicKey.ToPemString());
        }

    }

}
