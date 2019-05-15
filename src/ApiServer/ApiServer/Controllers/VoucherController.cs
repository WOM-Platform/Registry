using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api.Models;

namespace WomPlatform.Web.Api.Controllers {

    [Produces("application/json")]
    [Route("api/v1/voucher")]
    public class VoucherController : BaseRegistryController {

        public VoucherController(
            IConfiguration configuration,
            DatabaseOperator database,
            KeyManager keyManager,
            CryptoProvider crypto,
            ILogger<VoucherController> logger)
        : base(configuration, crypto, keyManager, database, logger)
        {
        }

        // POST api/v1/voucher/create
        [HttpPost("create")]
        public ActionResult Create([FromBody]VoucherCreatePayload payload) {
            Logger.LogDebug(LoggingEvents.VoucherCreation, "Received voucher creation from Source ID {0} with nonce {1}",
                payload.SourceId, payload.Nonce
            );

            var source = Database.GetSourceById(payload.SourceId);
            if(source == null) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Source ID {0} does not exist", payload.SourceId);
                return this.SourceNotFound();
            }

            var sourcePublicKey = KeyManager.LoadKeyFromString<AsymmetricKeyParameter>(source.PublicKey);

            (var payloadContent, var decryptResult) = ExtractInputPayload<VoucherCreatePayload.Content>(payload.Payload, LoggingEvents.VoucherCreation);
            if(decryptResult != null) {
                return decryptResult;
            }

            if(payload.SourceId != payloadContent.SourceId) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Verification failed, source ID {0} differs from ID {1} in payload", payload.SourceId, payloadContent.SourceId);
                return this.PayloadVerificationFailure("Verification of source ID in payload failed");
            }
            if(payload.Nonce != payloadContent.Nonce) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Verification failed, nonce {0} differs from nonce {1} in payload", payload.Nonce, payloadContent.Nonce);
                return this.PayloadVerificationFailure("Verification of nonce in payload failed");
            }
            if(!CheckPasswordValidity(payloadContent.Password)) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Password '{0}' unacceptable", payloadContent.Password);
                return this.PasswordUnacceptableFailure();
            }

            try {
                var otc = Database.CreateVoucherGeneration(payloadContent);

                Logger.LogInformation(LoggingEvents.VoucherCreation, "Voucher generation successfully requested with code {0} for source {1}", otc, payload.SourceId);

                return Ok(new VoucherCreateResponse {
                    Payload = Crypto.Encrypt(new VoucherCreateResponse.Content {
                        RegistryUrl = "https://wom.social",
                        Nonce = payloadContent.Nonce,
                        Otc = otc
                    }, sourcePublicKey)
                });
            }
            catch(Exception ex) {
                Logger.LogError(LoggingEvents.VoucherCreation, ex, "Failed to request vouchers");
                return this.UnexpectedError();
            }
        }

        [HttpPost("verify")]
        public ActionResult Verify([FromBody]VoucherVerifyPayload payload) {
            Logger.LogDebug(LoggingEvents.VoucherVerification, "Received voucher generation verification request");

            (var payloadContent, var decryptResult) = ExtractInputPayload<VoucherVerifyPayload.Content>(payload.Payload, LoggingEvents.VoucherVerification);
            if(decryptResult != null) {
                return decryptResult;
            }

            try {
                Database.VerifyGenerationRequest(payloadContent.Otc);

                Logger.LogInformation(LoggingEvents.VoucherVerification, "Voucher generation {0} verified", payloadContent.Otc);

                return Ok();
            }
            catch(ArgumentException ex) {
                Logger.LogError(LoggingEvents.VoucherVerification, ex, "Cannot verify voucher generation {0}", payloadContent.Otc);
                return this.ProblemParameter(ex.Message);
            }
            catch(Exception ex) {
                Logger.LogError(LoggingEvents.VoucherVerification, ex, "Failed to verify vouchers");
                return this.UnexpectedError();
            }
        }

        // POST api/v1/voucher/redeem
        [HttpPost("redeem")]
        public ActionResult Redeem([FromBody]VoucherRedeemPayload payload) {
            Logger.LogDebug("Received voucher redemption request");

            (var payloadContent, var decryptResult) = ExtractInputPayload<VoucherRedeemPayload.Content>(payload.Payload, LoggingEvents.VoucherRedemption);
            if(decryptResult != null) {
                return decryptResult;
            }

            byte[] ks = payloadContent.SessionKey.FromBase64();
            if(ks.Length != 32) {
                Logger.LogError(LoggingEvents.VoucherRedemption, "Insufficient session key length ({0} bytes)", ks.Length);
                return this.ProblemParameter($"Length of {nameof(payloadContent.SessionKey)} not valid");
            }

            try {
                Logger.LogDebug(LoggingEvents.VoucherRedemption, "Redeeming vouchers for request {0}", payloadContent.Otc);

                var (source, vouchers) = Database.GenerateVouchers(payloadContent.Otc, payloadContent.Password);

                Logger.LogInformation("Successfully redeemed vouchers by source {0}", source.Id);

                var content = new VoucherRedeemResponse.Content {
                    SourceId = source.Id,
                    SourceName = source.Name,
                    Vouchers = (from v in vouchers
                                select new VoucherRedeemResponse.VoucherInfo {
                                    Id = v.Id,
                                    Secret = Convert.ToBase64String(v.Secret),
                                    Aim = v.AimCode,
                                    Latitude = v.Latitude,
                                    Longitude = v.Longitude,
                                    Timestamp = v.Timestamp
                                }).ToArray()
                };

                return Ok(new VoucherRedeemResponse {
                    Payload = Crypto.Encrypt(content, ks)
                });
            }
            catch(ArgumentException ex) {
                Logger.LogError(LoggingEvents.VoucherRedemption, ex, "Generation request parameter not valid");
                return this.ProblemParameter(ex.Message);
            }
            catch(InvalidOperationException ex) {
                Logger.LogError(LoggingEvents.VoucherRedemption, ex, "Cannot perform voucher generation");
                return this.RequestVoid(ex.Message);
            }
            catch(Exception ex) {
                Logger.LogError(LoggingEvents.VoucherRedemption, ex, "Failed to redeem vouchers");
                return this.UnexpectedError();
            }
        }

    }

}
