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
    public class VoucherController : Controller {

        protected readonly IConfiguration _configuration;
        protected readonly DatabaseManager _database;
        protected readonly CryptoProvider _crypto;
        protected readonly ILogger<VoucherController> _logger;

        public VoucherController(IConfiguration configuration, DatabaseManager database, CryptoProvider cryptoProvider, ILogger<VoucherController> logger) {
            this._configuration = configuration;
            this._database = database;
            this._crypto = cryptoProvider;
            this._logger = logger;
        }

        // POST api/v1/voucher/create
        [HttpPost("create")]
        public VoucherCreateResponse Create([FromBody]VoucherCreatePayload payload) {
            this._logger.LogInformation(LoggingEvents.VoucherCreation, "Received create request from Source ID {0}, nonce {1}",
                payload.SourceId, payload.Nonce
            );

            var source = this._database.Connection.GetSourceById(payload.SourceId);
            if(source == null) {
                this._logger.LogError(LoggingEvents.VoucherCreation, "Source ID {0} does not exist", payload.SourceId);
                // TODO: correct error handling
                return null;
            }

            var sourcePublicKey = KeyManager.LoadKeyFromString<AsymmetricKeyParameter>(source.PublicKey);

            // Conversion from crypto-payload
            var payloadContent = this._crypto.DecryptPayload<VoucherCreatePayloadContent>(payload.Payload, sourcePublicKey);
            if(payload.SourceId != payloadContent.SourceId) {
                this._logger.LogError(LoggingEvents.VoucherCreation, "Verification failed, source ID {0} differs from ID {1} in payload", payload.SourceId, payloadContent.SourceId);
                // TODO
                return null;
            }
            if(payload.Nonce != payloadContent.Nonce) {
                this._logger.LogError(LoggingEvents.VoucherCreation, "Verification failed, nonce {0} differs from nonce {1} in payload", payload.Nonce, payloadContent.Nonce);
                // TODO
                return null;
            }

            this._logger.LogInformation(LoggingEvents.VoucherCreation, "Processing voucher generation for source {0}, nonce {1}", payload.SourceId, payload.Nonce);

            var otc = this._database.Connection.CreateVoucherGeneration(payloadContent);

            this._logger.LogTrace(LoggingEvents.VoucherCreation, "Voucher generation instance created with OTC {0}", otc);

            return new VoucherCreateResponse {
                EncryptedOtc = this._crypto.EncryptString(otc.ToString("D"), sourcePublicKey),
                VoucherAmount = payloadContent.Vouchers.Length
            };
        }

        // POST api/v1/voucher/redeem/{OTC}
        [HttpPost("redeem/{otc}")]
        public ActionResult Redeem([FromRoute]string otc, [FromBody]VoucherRedeemPayload payload) {
            this._logger.LogInformation(LoggingEvents.VoucherRedemption, "Received redeem request with OTC {0}",
                otc
            );

            if(!Guid.TryParseExact(otc, "N", out Guid otcGen)) {
                return this.NotFound();
            }

            try {
                var vouchers = this._database.Connection.GenerateVouchers(otcGen);
                var converted = (from v in vouchers
                                 select new VoucherRedeemResponse.VoucherInfo {
                                     Id = v.Id,
                                     Secret = Convert.ToBase64String(v.Secret),
                                     Latitude = v.Latitude,
                                     Longitude = v.Longitude,
                                     Source = "https://wom.social/sources/" + v.SourceId,
                                     Timestamp = v.Timestamp
                                 });

                return this.Ok(new VoucherRedeemResponse {
                    Nonce = payload.Nonce,
                    Vouchers = converted.ToArray()
                });
            }
            catch(Exception ex) {
                this._logger.LogError(LoggingEvents.VoucherRedemption, ex, "Failed to generate vouchers");

                throw;
            }
        }

    }

}
