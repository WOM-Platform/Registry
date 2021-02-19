﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Controllers {

    /// <summary>
    /// Methods used to provide authentication and key exchange for clients.
    /// </summary>
    [Route("v1/auth")]
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

        private User GetApiLoginUser() {
            if(User == null)
                return null;
            if(!(User.Identity is WomUserIdentity))
                return null;
            return ((WomUserIdentity)User.Identity).WomUser;
        }

        /// <summary>
        /// Retrieves available WOM sources for the authenticated user.
        /// </summary>
        [HttpGet("sources")]
        [Produces("application/json")]
        [RequireHttps]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        public async Task<IActionResult> SourceLoginV1() {
            Logger.LogDebug("Source login V1");

            var user = GetApiLoginUser();
            if(user == null) {
                return Forbid();
            }

            var sources = await Mongo.GetSourcesByUser(user.Id);
            Logger.LogInformation("User {0} has {1} source entries", user.Id, sources.Count);

            return Ok(new {
                Sources = from s in sources
                          select new {
                              id = s.Id,
                              name = s.Name,
                              url = s.Url,
                              privateKey = s.PrivateKey
                          }
            });
        }

        /// <summary>
        /// Retrieves available WOM POS instances for the authenticated user.
        /// </summary>
        [HttpGet("pos")]
        [Produces("application/json")]
        [RequireHttps]
        [Authorize(Policy = Startup.SimpleAuthPolicy)]
        public async Task<IActionResult> PosLoginV1() {
            Logger.LogDebug("POS login V1");

            var user = GetApiLoginUser();
            if(user == null) {
                return Forbid();
            }

            var pos = await Mongo.GetPosByUser(user.Id);
            Logger.LogInformation("User {0} has {1} POS entries", user.Id, pos.Count);

            return Ok(new {
                POS = from p in pos
                      select new {
                          id = p.Id,
                          name = p.Name,
                          url = p.Url,
                          privateKey = p.PrivateKey
                      }
            });
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
