using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api.Models;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/v1/voucher")]
    public class VoucherController : ControllerBase {

        public VoucherController(
            IConfiguration configuration,
            DatabaseManager databaseManager,
            CryptoProvider cryptoProvider,
            KeyManager keyManager,
            ILogger<VoucherController> logger)
        {
            Configuration = configuration;
            Database = databaseManager;
            Crypto = cryptoProvider;
            KeyManager = keyManager;
            Logger = logger;
        }

        protected IConfiguration Configuration { get; }
        protected DatabaseManager Database { get; }
        protected CryptoProvider Crypto { get; }
        protected KeyManager KeyManager { get; }
        protected ILogger<VoucherController> Logger { get; }

        // POST api/v1/voucher/create
        [HttpPost("create")]
        public VoucherCreateResponse Create([FromBody]VoucherCreatePayload payload) {
            Logger.LogInformation(LoggingEvents.VoucherCreation, "Received create request from Source ID {0}, nonce {1}",
                payload.SourceId, payload.Nonce
            );

            var source = Database.Connection.GetSourceById(payload.SourceId);
            if(source == null) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Source ID {0} does not exist", payload.SourceId);
                // TODO: correct error handling
                return null;
            }

            var sourcePublicKey = KeyManager.LoadKeyFromString<AsymmetricKeyParameter>(source.PublicKey);

            var payloadContent = Crypto.DecryptAndVerify<VoucherCreatePayload.Content>(payload.Payload,
                sourcePublicKey, KeyManager.RegistryPrivateKey);

            if(payload.SourceId != payloadContent.SourceId) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Verification failed, source ID {0} differs from ID {1} in payload", payload.SourceId, payloadContent.SourceId);
                // TODO
                return null;
            }
            if(payload.Nonce != payloadContent.Nonce) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Verification failed, nonce {0} differs from nonce {1} in payload", payload.Nonce, payloadContent.Nonce);
                // TODO
                return null;
            }
            // TODO: check whether nonce is valid

            var nextNonce = "todo";

            Logger.LogInformation(LoggingEvents.VoucherCreation, "Processing voucher generation for source {0}, nonce {1}", payload.SourceId, payload.Nonce);

            var otc = Database.Connection.CreateVoucherGeneration(payloadContent);

            Logger.LogTrace(LoggingEvents.VoucherCreation, "Voucher generation instance created with OTC {0}", otc);

            return new VoucherCreateResponse {
                Payload = Crypto.SignAndEncrypt(new VoucherCreateResponse.Content {
                    Source = UrlGenerator.GenerateSourceUrl(source.Id),
                    NextNonce = nextNonce,
                    Otc = otc
                }, KeyManager.RegistryPrivateKey, sourcePublicKey)
            };
        }

        // POST api/v1/voucher/redeem
        [HttpPost("redeem")]
        public ActionResult Redeem([FromBody]VoucherRedeemPayload payload) {
            var payloadContent = Crypto.Decrypt<VoucherRedeemPayload.Content>(payload.Payload, KeyManager.RegistryPrivateKey);

            try {
                var vouchers = Database.Connection.GenerateVouchers(payloadContent.Otc);
                var converted = (from v in vouchers
                                 select new VoucherRedeemResponse.VoucherInfo {
                                     Id = v.Id,
                                     Secret = Convert.ToBase64String(v.Secret),
                                     Latitude = v.Latitude,
                                     Longitude = v.Longitude,
                                     Source = UrlGenerator.GenerateSourceUrl(v.SourceId),
                                     Timestamp = v.Timestamp
                                 });
                var content = new VoucherRedeemResponse.Content {
                    Vouchers = converted.ToArray()
                };

                return Ok(new VoucherRedeemResponse {
                    Payload = Crypto.Sign(content, KeyManager.RegistryPrivateKey)
                });
            }
            catch(Exception ex) {
                Logger.LogError(LoggingEvents.VoucherRedemption, ex, "Failed to generate vouchers");

                throw;
            }
        }

    }

}
