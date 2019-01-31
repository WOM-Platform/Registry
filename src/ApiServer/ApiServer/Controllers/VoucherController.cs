using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api;
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
        public ActionResult Create([FromBody]VoucherCreatePayload payload) {
            Logger.LogDebug(LoggingEvents.VoucherCreation, "Received create request from Source ID {0}, nonce {1}",
                payload.SourceId, payload.Nonce
            );

            var source = Database.Context.GetSourceById(payload.SourceId);
            if(source == null) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Source ID {0} does not exist", payload.SourceId);
                return this.SourceNotFound();
            }

            var sourcePublicKey = KeyManager.LoadKeyFromString<AsymmetricKeyParameter>(source.PublicKey);

            var payloadContent = Crypto.DecryptAndVerify<VoucherCreatePayload.Content>(payload.Payload,
                sourcePublicKey, KeyManager.RegistryPrivateKey);

            if(payload.SourceId != payloadContent.SourceId) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Verification failed, source ID {0} differs from ID {1} in payload", payload.SourceId, payloadContent.SourceId);
                return this.PayloadVerificationFailure("Verification of source ID in payload failed");
            }
            if(payload.Nonce != payloadContent.Nonce) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Verification failed, nonce {0} differs from nonce {1} in payload", payload.Nonce, payloadContent.Nonce);
                return this.PayloadVerificationFailure("Verification of nonce in payload failed");
            }

            Logger.LogInformation(LoggingEvents.VoucherCreation, "Processing voucher generation for source {0} and nonce {1}", payload.SourceId, payload.Nonce);

            try {
                var otc = Database.Context.CreateVoucherGeneration(payloadContent);

                Logger.LogDebug(LoggingEvents.VoucherCreation, "Voucher generation instance created with OTC {0}", otc);

                return Ok(new VoucherCreateResponse {
                    Payload = Crypto.SignAndEncrypt(new VoucherCreateResponse.Content {
                        RegistryUrl = "https://wom.social",
                        Nonce = payloadContent.Nonce,
                        Otc = otc
                    }, KeyManager.RegistryPrivateKey, sourcePublicKey)
                });
            }
            catch(Exception ex) {
                Logger.LogError(LoggingEvents.VoucherCreation, ex, "Failed to request vouchers");
                return this.UnexpectedError();
            }
        }

        }

        // POST api/v1/voucher/redeem
        [HttpPost("redeem")]
        public ActionResult Redeem([FromBody]VoucherRedeemPayload payload) {
            var payloadContent = Crypto.Decrypt<VoucherRedeemPayload.Content>(payload.Payload, KeyManager.RegistryPrivateKey);

            byte[] ks = payloadContent.SessionKey.FromBase64();
            if(ks.Length != 32) {
                Logger.LogError(LoggingEvents.VoucherRedemption, "Insufficient session key length ({0} bytes)", ks.Length);
                return this.ProblemParameter($"Length of {nameof(payloadContent.SessionKey)} not valid");
            }

            try {
                var vouchers = Database.Context.GenerateVouchers(payloadContent.Otc, payloadContent.Password);

                var content = new VoucherRedeemResponse.Content {
                    Vouchers = (from v in vouchers
                                select new VoucherRedeemResponse.VoucherInfo {
                                    Id = v.Id,
                                    Secret = Convert.ToBase64String(v.Secret),
                                    Aim = UrlGenerator.GenerateAimUrl(v.AimCode),
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
                return this.RequestVoid("Voucher generation already requested");
            }
            catch(Exception ex) {
                Logger.LogError(LoggingEvents.VoucherRedemption, ex, "Failed to generate vouchers");
                return this.UnexpectedError();
            }
        }

    }

}
