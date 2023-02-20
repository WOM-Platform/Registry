using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Org.BouncyCastle.Crypto;
using WomPlatform.Connector.Models;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/payment")]
    [OperationsTags("Payment protocol", "Operations")]
    public class PaymentController : BaseRegistryController {

        public PaymentController(
            IServiceProvider serviceProvider,
            ILogger<AdminController> logger)
        : base(serviceProvider, logger) {
        }

        /// <summary>
        /// Registers a new payment request.
        /// </summary>
        /// <param name="payload">Payment request payload.</param>
        [HttpPost("register")]
        [ProducesResponseType(typeof(PaymentRegisterResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Register(
            [FromBody] PaymentRegisterPayload payload
        ) {
            if(payload == null || payload.Nonce == null) {
                Logger.LogDebug(LoggingEvents.PaymentCreation, "Payload or nonce void, aborting");

                return BadRequest();
            }
            
            Logger.LogInformation(LoggingEvents.PaymentCreation, "Received payment creation request from POS ID {0} with nonce {1}",
                payload.PosId, payload.Nonce
            );

            var pos = await PosService.GetPosById(new ObjectId(payload.PosId));
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
                var request = await PaymentService.CreatePaymentRequest(pos, payloadContent.Amount,
                    payloadContent.SimpleFilter.ToDocument(),
                    payloadContent.Password,
                    payloadContent.Nonce,
                    payloadContent.PocketAckUrl,
                    payloadContent.PosAckUrl,
                    payloadContent.Persistent
                );

                Logger.LogInformation(LoggingEvents.PaymentCreation, "Payment request successfully created with code {0} for POS {1}", request.Otc, payload.PosId);

                return Ok(new PaymentRegisterResponse {
                    Payload = Crypto.Encrypt(new PaymentRegisterResponse.Content {
                        RegistryUrl = $"https://{SelfHostDomain}",
                        Nonce = payloadContent.Nonce,
                        Otc = request.Otc,
                        Password = request.Password,
                        Link = $"https://{SelfLinkDomain}/payment/{request.Otc:D}"
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
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
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
                await PaymentService.VerifyPaymentRequest(payloadContent.Otc);

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
        [ProducesResponseType(typeof(PaymentInfoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
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
                var payment = await PaymentService.GetPaymentRequestByOtc(payloadContent.Otc);
                if(payment == null) {
                    Logger.LogInformation("Payment {0} not found", payloadContent.Otc);
                    return this.OtcNotFound();
                }

                var pos = await PosService.GetPosById(payment.PosId);

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
        [ProducesResponseType(typeof(PaymentConfirmResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
                var payment = await PaymentService.ProcessPayment(payloadContent);

                Logger.LogInformation("Successfully processed payment {0}", payment.Otc);

                return Ok(new PaymentConfirmResponse {
                    Payload = Crypto.Encrypt(new PaymentConfirmResponse.Content {
                        AckUrl = payment.AckUrlPocket
                    }, ks)
                });
            }
            catch (ArgumentException ex) {
                Logger.LogError(LoggingEvents.PaymentProcessing, ex, "Payment parameter not valid");
                return this.ProblemParameter(ex.Message);
            }
            catch (InvalidOperationException ex) {
                Logger.LogError(LoggingEvents.PaymentProcessing, ex, "Payment in invalid status");
                return this.RequestVoid(ex.Message);
            }
            catch(ServiceProblemException ex) {
                Logger.LogError(LoggingEvents.PaymentProcessing, ex, "Failed to perform payment operation");
                return ex.ToActionResult();
            }
            catch (Exception ex) {
                Logger.LogError(LoggingEvents.PaymentProcessing, ex, "Failed to process payment");
                return this.UnexpectedError();
            }
        }

        /// <summary>
        /// Retrieves status information about a payment.
        /// </summary>
        /// <param name="payload">Payment confirmation payload.</param>
        [HttpPost("status")]
        [ProducesResponseType(typeof(PaymentStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStatus(
            [FromBody] PaymentStatusPayload payload
        ) {
            if(payload == null) {
                return BadRequest();
            }

            Logger.LogDebug(LoggingEvents.PaymentStatus, "Received status request");

            (var payloadContent, var decryptResult) = ExtractInputPayload<PaymentStatusPayload.Content>(payload.Payload, LoggingEvents.PaymentStatus);
            if(decryptResult != null) {
                return decryptResult;
            }

            if(payload.PosId != payloadContent.PosId) {
                Logger.LogError(LoggingEvents.PaymentStatus, "POS ID mismatch in payload ({0} != {1})", payload.PosId, payloadContent.PosId);
                return BadRequest();
            }

            try {
                var payment = await PaymentService.GetPaymentRequestByOtc(payloadContent.Otc);
                if(payment == null) {
                    Logger.LogInformation("Payment {0} not found", payloadContent.Otc);
                    return this.OtcNotFound();
                }
                if(payment.PosId != payment.PosId) {
                    Logger.LogWarning(LoggingEvents.PaymentStatus, "Payment {0} has not been created by POS {1}", payment.Otc, payment.PosId);
                    return this.OtcNotFound();
                }

                var pos = await PosService.GetPosById(payment.PosId);
                if(pos == null) {
                    Logger.LogWarning(LoggingEvents.PaymentStatus, "POS of payment {0} does not exist", payment.Otc);
                    return this.PosNotFound();
                }

                var posPublicKey = CryptoHelper.LoadKeyFromString<AsymmetricKeyParameter>(pos.PublicKey);

                Logger.LogInformation(LoggingEvents.PaymentStatus, "Retrieved status of payment {0}", payloadContent.Otc);

                return Ok(new PaymentStatusResponse {
                    PosId = payload.PosId,
                    Payload = Crypto.Encrypt(new PaymentStatusResponse.Content {
                        Persistent = payment.IsPersistent,
                        HasBeenPerformed = (payment.Confirmations != null && payment.Confirmations.Count > 0),
                        Confirmations = (from c in payment.Confirmations ?? new()
                                         select new PaymentStatusResponse.Confirmation {
                                             PerformedAt = DateTime.SpecifyKind(c.PerformedAt, DateTimeKind.Utc)
                                         }).ToList(),
                    }, posPublicKey)
                });
            }
            catch(Exception ex) {
                Logger.LogError(LoggingEvents.PaymentStatus, ex, "Failed to retrieve payment status");
                return this.UnexpectedError();
            }
        }

    }

}
