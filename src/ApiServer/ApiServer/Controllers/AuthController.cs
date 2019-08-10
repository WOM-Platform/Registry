using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api.DatabaseModels;
using WomPlatform.Web.Api.Models;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/v1/auth")]
    [XForwardedProto("https")]
    public class AuthController : BaseRegistryController {

        public AuthController(
            IConfiguration configuration,
            DatabaseOperator database,
            KeyManager keyManager,
            CryptoProvider crypto,
            ILogger<AuthController> logger)
        : base(configuration, crypto, keyManager, database, logger) {
        }

        private User GetUserProfile() {
            if(User == null)
                return null;
            if(!(User.Identity is WomUserIdentity))
                return null;
            return ((WomUserIdentity)User.Identity).WomUser;
        }

        // GET api/v1/auth/sources
        [HttpGet("sources")]
        [Produces("application/json")]
        [Authorize]
        public ActionResult Sources() {
            Logger.LogDebug("Retrieving user sources");

            var user = GetUserProfile();
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
        }

        // GET api/v1/auth/pos
        [HttpGet("pos")]
        [Produces("application/json")]
        [Authorize]
        public ActionResult Pos() {
            Logger.LogDebug("Retrieving user POS");

            var user = GetUserProfile();
            if(user == null) {
                return Forbid();
            }

            return Ok(new {
                POS = from s in Database.GetPosByUser(user)
                          select new {
                              s.Id,
                              s.Name,
                              s.Url,
                              s.PrivateKey
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
