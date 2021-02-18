using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Org.BouncyCastle.Crypto;
using WomPlatform.Connector;
using WomPlatform.Connector.Models;

namespace WomPlatform.Web.Api.Controllers {

    [Produces("application/json")]
    [Route("v1/payment")]
    [OperationsTags("Payment protocol", "Operations")]
    public class PaymentController : BaseRegistryController {

        public PaymentController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<PaymentController> logger)
        : base(configuration, crypto, keyManager, mongo, @operator, logger) {

        }

        /// <summary>
        /// Registers a new payment request.
        /// </summary>
        /// <param name="payload">Payment request payload.</param>
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] PaymentRegisterPayload payload
        ) {
            if(payload == null || payload.Nonce == null) {
                return BadRequest();
            }

            Logger.LogInformation(LoggingEvents.PaymentCreation, "Received payment creation request from POS ID {0} with nonce {1}",
                payload.PosId, payload.Nonce
            );

            var pos = await Mongo.GetPosById(new ObjectId(payload.PosId));
            if (pos == null) {
                Logger.LogError(LoggingEvents.PaymentCreation, "Source ID {0} does not exist", payload.PosId);
                return this.PosNotFound();
            }
            var posPublicKey = CryptoHelper.LoadKeyFromString<AsymmetricKeyParameter>(pos.PublicKey);

            (var payloadContent, var decryptResult) = ExtractInputPayload<PaymentRegisterPayload.Content>(payload.Payload, LoggingEvents.PaymentCreation);
            if(decryptResult != null) {
                return decryptResult;
            }

            if (payload.PosId != payloadContent.PosId) {
                Logger.LogError(LoggingEvents.PaymentCreation, "Verification failed, POS ID {0} differs from ID {1} in payload", payload.PosId, payloadContent.PosId);
                return this.PayloadVerificationFailure("Verification of POS ID in payload failed");
            }
            if (payload.Nonce != payloadContent.Nonce) {
                Logger.LogError(LoggingEvents.PaymentCreation, "Verification failed, nonce {0} differs from nonce {1} in payload", payload.Nonce, payloadContent.Nonce);
                return this.PayloadVerificationFailure("Verification of nonce in payload failed");
            }
            if(!CheckTransferPassword(payloadContent.Password)) {
                Logger.LogError(LoggingEvents.PaymentCreation, "Password '{0}' unacceptable", payloadContent.Password);
                return this.PasswordUnacceptableFailure();
            }

            try {
                (var otc, var password) = await Operator.CreatePaymentRequest(pos, payloadContent);

                Logger.LogInformation(LoggingEvents.PaymentCreation, "Payment request successfully created with code {0} for POS {1}", otc, payload.PosId);

                return Ok(new PaymentRegisterResponse {
                    Payload = Crypto.Encrypt(new PaymentRegisterResponse.Content {
                        RegistryUrl = $"https://{SelfHostDomain}",
                        Nonce = payloadContent.Nonce,
                        Otc = otc,
                        Password = password
                    }, posPublicKey)
                });
            }
            catch (Exception ex) {
                Logger.LogError(LoggingEvents.PaymentCreation, ex, "Failed to create payment");
                return this.UnexpectedError();
            }
        }

        /// <summary>
        /// Verifies and activates an existing payment request.
        /// </summary>
        /// <param name="payload">Payment verification payload.</param>
        [HttpPost("verify")]
        public async Task<IActionResult> Verify(
            [FromBody] PaymentVerifyPayload payload
        ) {
            if(payload == null) {
                return BadRequest();
            }

            Logger.LogDebug(LoggingEvents.PaymentVerification, "Received verification request");

            (var payloadContent, var decryptResult) = ExtractInputPayload<PaymentVerifyPayload.Content>(payload.Payload, LoggingEvents.PaymentVerification);
            if(decryptResult != null) {
                return decryptResult;
            }

            try {
                await Operator.VerifyPaymentRequest(payloadContent.Otc);

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

        /// <summary>
        /// Retrieves information about an existing and activated payment request.
        /// </summary>
        /// <param name="payload">Payment information request payload.</param>
        [HttpPost("info")]
        public async Task<IActionResult> GetInformation(
            [FromBody] PaymentInfoPayload payload
        ) {
            if(payload == null) {
                return BadRequest();
            }

            Logger.LogDebug("Received payment information request");

            (var payloadContent, var decryptResult) = ExtractInputPayload<PaymentInfoPayload.Content>(payload.Payload, LoggingEvents.PaymentInformationAccess);
            if(decryptResult != null) {
                return decryptResult;
            }

            byte[] ks = payloadContent.SessionKey.FromBase64();
            if (ks.Length != 32) {
                Logger.LogError(LoggingEvents.PaymentInformationAccess, "Insufficient session key length ({0} bytes)", ks.Length);
                return this.ProblemParameter($"Length of {nameof(payloadContent.SessionKey)} not valid");
            }

            try {
                var payment = await Mongo.GetPaymentRequestByOtc(payloadContent.Otc);
                if(payment == null) {
                    Logger.LogInformation("Payment {0} not found", payloadContent.Otc);
                    return NotFound();
                }

                var pos = await Mongo.GetPosById(payment.PosId);

                Logger.LogInformation("Information request for payment {0} from POS {1} for {2} vouchers", payment.Otc, pos.Id, payment.Amount);

                var content = new PaymentInfoResponse.Content {
                    Amount = payment.Amount,
                    PosId = pos.Id.ToString(),
                    PosName = pos.Name ?? "Unknown",
                    SimpleFilter = payment.Filter.ToSimpleFilter(),
                    Persistent = payment.IsPersistent
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

        /// <summary>
        /// Confirms and executes a payment.
        /// This voids all vouchers passed in payment and confirms the payment to the originating POS.
        /// </summary>
        /// <param name="payload">Payment confirmation payload.</param>
        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm(
            [FromBody] PaymentConfirmPayload payload
        ) {
            if(payload == null) {
                return BadRequest();
            }

            Logger.LogDebug("Received payment confirmation request");

            (var payloadContent, var decryptResult) = ExtractInputPayload<PaymentConfirmPayload.Content>(payload.Payload, LoggingEvents.PaymentProcessing);
            if(decryptResult != null) {
                return decryptResult;
            }

            byte[] ks = payloadContent.SessionKey.FromBase64();
            if (ks.Length != 32) {
                Logger.LogError(LoggingEvents.PaymentProcessing, "Insufficient session key length ({0} bytes)", ks.Length);
                return this.ProblemParameter($"Length of {nameof(payloadContent.SessionKey)} not valid");
            }

            try {
                var payment = await Operator.ProcessPayment(payloadContent);

                Logger.LogInformation("Successfully processed payment {0}", payment.Otc);

                return Ok(new PaymentConfirmResponse {
                    Payload = Crypto.Encrypt(new PaymentConfirmResponse.Content {
                        AckUrl = payment.AckUrlPocket
                    }, ks)
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
                Logger.LogError(LoggingEvents.PaymentInformationAccess, ex, "Failed to process payment");
                return this.UnexpectedError();
            }
        }

    }

}
