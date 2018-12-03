using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api.DatabaseModels;
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
            this.Configuration = configuration;
            this.Database = databaseManager;
            this.Crypto = cryptoProvider;
            this.KeyManager = keyManager;
            this.Logger = logger;
        }

        protected IConfiguration Configuration { get; }
        protected DatabaseManager Database { get; }
        protected CryptoProvider Crypto { get; }
        protected KeyManager KeyManager { get; }
        protected ILogger<VoucherController> Logger { get; }

        // POST api/v1/voucher/create
        [HttpPost("create")]
        public VoucherCreateResponse Create([FromBody]VoucherCreatePayload payload) {
            this.Logger.LogInformation(LoggingEvents.VoucherCreation, "Received create request from Source ID {0}, nonce {1}",
                payload.SourceId, payload.Nonce
            );

            var source = this.Database.Connection.GetSourceById(payload.SourceId);
            if(source == null) {
                this.Logger.LogError(LoggingEvents.VoucherCreation, "Source ID {0} does not exist", payload.SourceId);
                // TODO: correct error handling
                return null;
            }

            var sourcePublicKey = KeyManager.LoadKeyFromString<AsymmetricKeyParameter>(source.PublicKey);

            var payloadContent = this.Crypto.DecryptAndVerify<VoucherCreatePayload.Content>(payload.Payload,
                sourcePublicKey, this.KeyManager.RegistryPublicKey);

            if(payload.SourceId != payloadContent.SourceId) {
                this.Logger.LogError(LoggingEvents.VoucherCreation, "Verification failed, source ID {0} differs from ID {1} in payload", payload.SourceId, payloadContent.SourceId);
                // TODO
                return null;
            }
            if(payload.Nonce != payloadContent.Nonce) {
                this.Logger.LogError(LoggingEvents.VoucherCreation, "Verification failed, nonce {0} differs from nonce {1} in payload", payload.Nonce, payloadContent.Nonce);
                // TODO
                return null;
            }
            // TODO: check whether nonce is valid

            var nextNonce = "todo";

            this.Logger.LogInformation(LoggingEvents.VoucherCreation, "Processing voucher generation for source {0}, nonce {1}", payload.SourceId, payload.Nonce);

            var otc = this.Database.Connection.CreateVoucherGeneration(payloadContent);

            this.Logger.LogTrace(LoggingEvents.VoucherCreation, "Voucher generation instance created with OTC {0}", otc);

            return new VoucherCreateResponse {
                Payload = this.Crypto.SignAndEncrypt(new VoucherCreateResponse.Content {
                    Source = UrlGenerator.GenerateSourceUrl(source.Id),
                    NextNonce = nextNonce,
                    Otc = otc
                }, KeyManager.RegistryPrivateKey, sourcePublicKey)
            };
        }

        // POST api/v1/voucher/redeem/{OTC}
        [HttpPost("redeem/{otc}")]
        public ActionResult Redeem([FromRoute]string otc, [FromBody]VoucherRedeemPayload payload) {
            this.Logger.LogInformation(LoggingEvents.VoucherRedemption, "Received redeem request with OTC {0}",
                otc
            );

            if(!Guid.TryParseExact(otc, "N", out Guid otcGen)) {
                return this.NotFound();
            }

            try {
                var vouchers = this.Database.Connection.GenerateVouchers(otcGen);
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
                    Nonce = payload.Nonce,
                    Vouchers = converted.ToArray()
                };

                return this.Ok(new VoucherRedeemResponse {
                    Payload = this.Crypto.Sign(content, this.KeyManager.RegistryPrivateKey)
                });
            }
            catch(Exception ex) {
                this.Logger.LogError(LoggingEvents.VoucherRedemption, ex, "Failed to generate vouchers");

                throw;
            }
        }

    }

}
