using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Pos;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Methods used to provide authentication and key exchange for clients.
    /// </summary>
    [Route("v1/auth")]
    [OperationsTags("Authentication")]
    [Obsolete]
    public class AuthController : BaseRegistryController {

        private readonly MongoDatabase _mongo;
        private readonly PosService _posService;

        public AuthController(
            MongoDatabase mongo,
            PosService posService,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, logger) {
            _mongo = mongo;
            _posService = posService;
        }

        public record AuthSourceLoginOutput(
            SourceLoginOutput[] Sources
        );

        /// <summary>
        /// Retrieves available WOM sources for the authenticated user.
        /// </summary>
        [HttpGet("sources")]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        [RequireHttpsInProd]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AuthSourceLoginOutput), StatusCodes.Status200OK)]
        public async Task<IActionResult> SourceLoginV1() {
            Logger.LogDebug("Source login V1");

            if(!User.GetUserId(out var userId)) {
                return Forbid();
            }

            var sources = await _mongo.GetSourcesByUser(userId);
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
            PosAuthOutput[] POS
        );

        /// <summary>
        /// Retrieves available WOM POS instances for the authenticated user.
        /// </summary>
        [HttpGet("pos")]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        [RequireHttpsInProd]
        [Produces("application/json")]
        [ProducesResponseType(typeof(AuthPosLoginOutput), StatusCodes.Status200OK)]
        public async Task<IActionResult> PosLoginV1() {
            Logger.LogDebug("POS login V1");

            if(!User.GetUserId(out var userId)) {
                return Forbid();
            }

            var pos = await _posService.GetPosByUser(userId);
            Logger.LogInformation("User {0} has {1} POS entries", userId, pos.Count);

            return Ok(new AuthPosLoginOutput(
                pos.Select(p => p.ToAuthOutput()).ToArray()
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

    }

}
