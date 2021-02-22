﻿using System.Linq;
using System.Security.Claims;
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

        public record AuthV2PosLoginOutput(
            string Name,
            string Surname,
            string Email,
            AuthMerchantLoginInfo[] Merchants
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
                data.Select(d => new AuthMerchantLoginInfo(
                    d.Item1.Id.ToString(),
                    d.Item1.Name,
                    d.Item1.FiscalCode,
                    d.Item1.Address,
                    d.Item1.ZipCode,
                    d.Item1.City,
                    d.Item1.Country,
                    d.Item1.WebsiteUrl,
                    d.Item2.Select(p => new AuthPosLoginInfo(
                        p.Id.ToString(),
                        p.Name,
                        p.Url,
                        p.PrivateKey,
                        p.PublicKey
                    )).ToArray()
                )).ToArray()
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

