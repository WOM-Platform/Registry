using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Controllers {

    [ApiController]
    [Route("api/v{version:apiVersion}/auth")]
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

        // GET /api/v1/auth/sources
        [HttpGet("sources")]
        [Produces("application/json")]
        [Authorize(Startup.ApiLoginPolicy)]
        [XForwardedProto("https")]
        [ApiVersion("1.0")]
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

        // GET /api/v1/auth/pos
        [HttpGet("pos")]
        [Produces("application/json")]
        [Authorize(Startup.ApiLoginPolicy)]
        [XForwardedProto("https")]
        [ApiVersion("1.0")]
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

        // POST /api/v2/auth/merchant
        [HttpPost("merchant")]
        [Produces("application/json")]
        [Authorize(Startup.ApiLoginPolicy)]
        [XForwardedProto("https")]
        [ApiVersion("2.0")]
        public async Task<IActionResult> PosLoginV2() {
            Logger.LogDebug("POS login V2");

            var user = GetApiLoginUser();
            if(user == null) {
                return Forbid();
            }

            var data = await Mongo.GetMerchantsAndPosByUser(user.Id);
            Logger.LogInformation("User {0} controls POS for {1} merchants", user.Id, data.Count);

            return Ok(new {
                user.Name,
                user.Surname,
                user.Email,
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

        [HttpGet("key")]
        [Produces("text/plain")]
        [ApiVersion("1.0")]
        [ApiVersion("2.0")]
        public IActionResult GetPublicKey() {
            return Ok(KeyManager.RegistryPublicKey.ToPemString());
        }

    }

}
