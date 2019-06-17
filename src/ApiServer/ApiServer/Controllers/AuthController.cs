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

    [Produces("application/json")]
    [Route("api/v1/auth")]
    [Authorize]
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
        public ActionResult Sources() {
            Logger.LogInformation("Listing user's sources");

            var user = GetUserProfile();
            if(user == null) {
                return Forbid();
            }

            return Ok(new {
                Sources = from s in Database.GetSourcesByUser(user)
                          select new {
                              Id = s.Id,
                              Name = s.Name,
                              Url = s.Url,
                              PublicKey = s.PublicKey
                          }
            });
        }

        // GET api/v1/auth/pos
        [HttpGet("pos")]
        public ActionResult Pos() {
            Logger.LogInformation("Listing user's POS");

            var user = GetUserProfile();
            if(user == null) {
                return Forbid();
            }

            return Ok(new {
                POS = from s in Database.GetPosByUser(user)
                          select new {
                              Id = s.Id,
                              Name = s.Name,
                              Url = s.Url,
                              PublicKey = s.PublicKey
                          }
            });
        }

    }

}
