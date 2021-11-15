using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.OutputModels;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Methods used to provide authentication and key exchange for clients.
    /// </summary>
    [ApiVersion("1")]
    [Route("v{version:apiVersion}/auth")]
    [OperationsTags("Authentication")]
    public class AuthController : BaseRegistryController {

        public AuthController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, mongo, @operator, logger) {
        }

        public record AuthSourceLoginOutput(
            SourceLoginOutput[] Sources
        );

        /// <summary>
        /// Retrieves available WOM sources for the authenticated user.
        /// </summary>
        [HttpGet("sources")]
        [Produces("application/json")]
        [RequireHttps]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        public async Task<IActionResult> SourceLoginV1() {
            Logger.LogDebug("Source login V1");

            if(!User.GetUserId(out var userId)) {
                return Forbid();
            }

            var sources = await Mongo.GetSourcesByUser(userId);
            Logger.LogInformation("User {0} has {1} source entries", userId, sources.Count);

            return Ok(new AuthSourceLoginOutput(
                sources.Select(s => new SourceLoginOutput {
                    Id = s.Id.ToString(),
                    Name = s.Name,
                    Url = s.Url,
                    PrivateKey = s.PrivateKey,
                    PublicKey = s.PublicKey
                }).ToArray()
            ));
        }

        public record AuthPosLoginOutput(
            PosLoginOutput[] POS
        );

        /// <summary>
        /// Retrieves available WOM POS instances for the authenticated user.
        /// </summary>
        [HttpGet("pos")]
        [Produces("application/json")]
        [RequireHttps]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        public async Task<IActionResult> PosLoginV1() {
            Logger.LogDebug("POS login V1");

            if(!User.GetUserId(out var userId)) {
                return Forbid();
            }

            var pos = await Mongo.GetPosByUser(userId);
            Logger.LogInformation("User {0} has {1} POS entries", userId, pos.Count);

            return Ok(new AuthPosLoginOutput(
                pos.Select(p => new PosLoginOutput {
                    Id = p.Id.ToString(),
                    Name = p.Name,
                    Url = p.Url,
                    PrivateKey = p.PrivateKey,
                    PublicKey = p.PublicKey
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
