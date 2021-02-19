using System.Linq;
using System.Security.Claims;
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
    [Route("v2/auth")]
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

            var data = await Mongo.GetMerchantsAndPosByUser(userId);
            Logger.LogInformation("User {0} controls POS for {1} merchants", userId, data.Count);

            return Ok(new {
                Email = User.FindFirst(ClaimTypes.Email).Value,
                Merchants = from m in data
                            select new {
                                m.Item1.Id,
                                m.Item1.Name,
                                m.Item1.FiscalCode,
                                m.Item1.Address,
                                m.Item1.ZipCode,
                                m.Item1.City,
                                m.Item1.Country,
                                Url = m.Item1.WebsiteUrl,
                                Pos = from p in m.Item2
                                      select new {
                                          p.Id,
                                          p.Name,
                                          p.PrivateKey,
                                          p.PublicKey
                                      }
                            }
            }
            );
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
