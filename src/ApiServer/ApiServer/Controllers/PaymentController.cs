using System;
using System.Data.Common;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using WomPlatform.Web.Api.Models;

namespace WomPlatform.Web.Api.Controllers {

    [Route("api/v1/payment")]
    public class PaymentController : ControllerBase {

        public PaymentController(
            IConfiguration configuration,
            DatabaseManager databaseManager,
            CryptoProvider cryptoProvider,
            KeyManager keyManager,
            ILogger<PaymentController> logger)
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
        protected ILogger<PaymentController> Logger { get; }

        // POST /api/v1/payment/register
        [HttpPost("register")]
        public ActionResult Register([FromBody]PaymentRegisterPayload payload) {
            Logger.LogDebug(LoggingEvents.PaymentCreation, "Received create request from POS ID {0}, nonce {1}",
                payload.PosId, payload.Nonce
            );

            var pos = Database.Context.GetPosById(payload.PosId);
            if (pos == null) {
                Logger.LogError(LoggingEvents.PaymentCreation, "Source ID {0} does not exist", payload.PosId);
                return this.PosNotFound();
            }

            var posPublicKey = KeyManager.LoadKeyFromString<AsymmetricKeyParameter>(pos.PublicKey);

            var payloadContent = Crypto.Decrypt<PaymentRegisterPayload.Content>(payload.Payload, KeyManager.RegistryPrivateKey);

            if (payload.PosId != payloadContent.PosId) {
                Logger.LogError(LoggingEvents.PaymentCreation, "Verification failed, POS ID {0} differs from ID {1} in payload", payload.PosId, payloadContent.PosId);
                return this.PayloadVerificationFailure("Verification of POS ID in payload failed");
            }
            if (payload.Nonce != payloadContent.Nonce) {
                Logger.LogError(LoggingEvents.PaymentCreation, "Verification failed, nonce {0} differs from nonce {1} in payload", payload.Nonce, payloadContent.Nonce);
                return this.PayloadVerificationFailure("Verification of nonce in payload failed");
            }

            Logger.LogInformation(LoggingEvents.PaymentCreation, "Processing payment creation for POS {0} and nonce {1}", payload.PosId, payload.Nonce);

            try {
                var otc = Database.Context.CreatePaymentRequest(payloadContent);

                Logger.LogDebug(LoggingEvents.PaymentCreation, "Payment instance created with OTC {0}", otc);

                return Ok(new PaymentRegisterResponse {
                    Payload = Crypto.Encrypt(new PaymentRegisterResponse.Content {
                        RegistryUrl = "https://wom.social",
                        Nonce = payloadContent.Nonce,
                        Otc = otc
                    }, posPublicKey)
                });
            }
            catch (Exception ex) {
                Logger.LogError(LoggingEvents.PaymentCreation, ex, "Failed to create payment");
                return this.UnexpectedError();
            }
        }

        [HttpPost("verify")]
        public ActionResult Verify([FromBody]PaymentVerifyPayload payload) {
            Logger.LogDebug(LoggingEvents.PaymentVerification, "Received verification request");

            var payloadContent = Crypto.Decrypt<PaymentVerifyPayload.Content>(payload.Payload, KeyManager.RegistryPrivateKey);

            try {
                Database.Context.VerifyPaymentRequest(payloadContent.Otc);
                Logger.LogInformation(LoggingEvents.PaymentVerification, "Payment creation {0} verified", payloadContent.Otc);

                return Ok();
            }
            catch (ArgumentException ex) {
                Logger.LogError(LoggingEvents.PaymentVerification, ex, "Cannot verify payment {0}", payloadContent.Otc);
                return this.ProblemParameter(ex.Message);
            }
            catch (Exception ex) {
                Logger.LogError(LoggingEvents.PaymentVerification, ex, "Failed to verify payment");
                return this.UnexpectedError();
            }
        }

        // POST /api/v1/payment/info
        [HttpPost("info")]
        public ActionResult GetInformation([FromBody]PaymentInfoPayload payload) {
            var payloadContent = Crypto.Decrypt<PaymentInfoPayload.Content>(payload.Payload, KeyManager.RegistryPrivateKey);

            byte[] ks = payloadContent.SessionKey.FromBase64();
            if (ks.Length != 32) {
                Logger.LogError(LoggingEvents.PaymentInformationAccess, "Insufficient session key length ({0} bytes)", ks.Length);
                return this.ProblemParameter($"Length of {nameof(payloadContent.SessionKey)} not valid");
            }

            try {
                (var payment, var filter) = Database.Context.GetPaymentRequestInfo(payloadContent.Otc, payloadContent.Password);

                var content = new PaymentInfoResponse.Content {
                    Amount = payment.Amount,
                    PosId = payment.Pos.Id,
                    SimpleFilter = filter?.Simple
                };

                return Ok(new PaymentInfoResponse {
                    Payload = Crypto.Encrypt(content, ks)
                });
            }
            catch (ArgumentException ex) {
                Logger.LogError(LoggingEvents.PaymentInformationAccess, ex, "Payment parameter not valid");
                return this.ProblemParameter(ex.Message);
            }
            catch (InvalidOperationException ex) {
                Logger.LogError(LoggingEvents.PaymentInformationAccess, ex, "Payment in invalid status");
                return this.RequestVoid(ex.Message);
            }
            catch (Exception ex) {
                Logger.LogError(LoggingEvents.PaymentInformationAccess, ex, "Failed to access payment information");
                return this.UnexpectedError();
            }
        }

        // POST /api/v1/payment/confirm
        [HttpPost("confirm")]
        public ActionResult Confirm([FromBody]PaymentConfirmPayload payload) {
            return Ok();
        }

    }
}
