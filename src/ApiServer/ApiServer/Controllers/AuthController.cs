using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/v1/auth")]
    public class AuthController : BaseRegistryController {

        public AuthController(
            IConfiguration configuration,
            MongoDatabase mongo,
            DatabaseOperator database,
            KeyManager keyManager,
            CryptoProvider crypto,
            ILogger<AuthController> logger)
        : base(configuration, crypto, keyManager, mongo, database, logger) {
        }

        private User GetApiLoginUser() {
            if(User == null)
                return null;
            if(!(User.Identity is WomUserIdentity))
                return null;
            return ((WomUserIdentity)User.Identity).WomUser;
        }

        // GET api/v1/auth/sources
        /*[HttpGet("sources")]
        [Produces("application/json")]
        [Authorize(Startup.ApiLoginPolicy)]
        [XForwardedProto("https")]
        public ActionResult Sources() {
            Logger.LogDebug("Retrieving user sources");

            var user = GetApiLoginUser();
            if(user == null) {
                return Forbid();
            }

            return Ok(new {
                Sources = from s in Database.GetSourcesByUser(user)
                          select new {
                              s.Id,
                              s.Name,
                              s.Url,
                              s.PrivateKey
                          }
            });
        }*/

        // GET api/v1/auth/pos
        [HttpGet("pos")]
        [Produces("application/json")]
        [Authorize(Startup.ApiLoginPolicy)]
        [XForwardedProto("https")]
        public async Task<IActionResult> Pos() {
            Logger.LogDebug("Retrieving user POS");

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

        [HttpGet("key")]
        [Produces("text/plain")]
        public ActionResult GetPublicKey() {
            return Ok(KeyManager.ConvertToString(KeyManager.RegistryPublicKey));
        }

    }

}
