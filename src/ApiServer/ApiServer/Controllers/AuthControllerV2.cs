using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v2/auth")]
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

        private User GetApiLoginUser() {
            if(User == null)
                return null;
            if(!(User.Identity is WomUserIdentity))
                return null;
            return ((WomUserIdentity)User.Identity).WomUser;
        }

        // POST /api/v2/auth/merchant
        [HttpPost("merchant")]
        [Produces("application/json")]
        [RequireHttps]
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
        public IActionResult GetPublicKey() {
            return Ok(KeyManager.RegistryPublicKey.ToPemString());
        }

    }

}
