using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Org.BouncyCastle.Crypto;
using WomPlatform.Connector;
using WomPlatform.Connector.Models;

namespace WomPlatform.Web.Api.Controllers {

    [Produces("application/json")]
    [Route("v1/voucher")]
    [OperationsTags("Voucher generation protocol", "Operations")]
    public class VoucherController : BaseRegistryController {

        public VoucherController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<BaseRegistryController> logger
        ) : base(configuration, crypto, keyManager, mongo, @operator, logger) {

        }

        /// <summary>
        /// Registers a new voucher generation request.
        /// </summary>
        /// <param name="payload">Voucher generation request payload.</param>
        [HttpPost("create")]
        public async Task<ActionResult> Create(
            [FromBody] VoucherCreatePayload payload
        ) {
            if(payload == null || payload.Nonce == null) {
                return BadRequest();
            }

            Logger.LogDebug(LoggingEvents.VoucherCreation, "Received voucher creation from Source ID {0} with nonce {1}",
                payload.SourceId, payload.Nonce
            );

            if(!ObjectId.TryParse(payload.SourceId.Id, out var sourceId)) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Source ID {0} is not valid", payload.SourceId);
                return this.ProblemParameter("Source ID is not valid");
            }

            var source = await Mongo.GetSourceById(sourceId);
            if(source == null) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Source ID {0} does not exist", payload.SourceId);
                return this.SourceNotFound();
            }

            var sourcePublicKey = CryptoHelper.LoadKeyFromString<AsymmetricKeyParameter>(source.PublicKey);

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
            if(!CheckTransferPassword(payloadContent.Password)) {
                Logger.LogError(LoggingEvents.VoucherCreation, "Password '{0}' unacceptable", payloadContent.Password);
                return this.PasswordUnacceptableFailure();
            }

            try {
                (var otc, var password) = await Operator.CreateGenerationRequest(source, payloadContent);

                Logger.LogInformation(LoggingEvents.VoucherCreation, "Voucher generation successfully requested with code {0} for source {1}", otc, payload.SourceId);

                return Ok(new VoucherCreateResponse {
                    Payload = Crypto.Encrypt(new VoucherCreateResponse.Content {
                        RegistryUrl = $"https://{SelfHostDomain}",
                        Nonce = payloadContent.Nonce,
                        Otc = otc,
                        Password = password
                    }, sourcePublicKey)
                });
            }
            catch(Exception ex) {
                Logger.LogError(LoggingEvents.VoucherCreation, ex, "Failed to request vouchers");
                return this.UnexpectedError();
            }
        }

        /// <summary>
        /// Verifies and activates an existing voucher generation request.
        /// </summary>
        /// <param name="payload">Voucher generation verification payload.</param>
        [HttpPost("verify")]
        public async Task<IActionResult> Verify(
            [FromBody] VoucherVerifyPayload payload
        ) {
            if(payload == null) {
                return BadRequest();
            }

            Logger.LogDebug(LoggingEvents.VoucherVerification, "Received voucher generation verification request");

            (var payloadContent, var decryptResult) = ExtractInputPayload<VoucherVerifyPayload.Content>(payload.Payload, LoggingEvents.VoucherVerification);
            if(decryptResult != null) {
                return decryptResult;
            }

            try {
                await Operator.VerifyGenerationRequest(payloadContent.Otc);

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

        /// <summary>
        /// Converts aggregate internal vouchers into "inflated" vouchers to be used through the v1 API.
        /// </summary>
        private IEnumerable<VoucherRedeemResponse.VoucherInfo> InflateVouchers(DatabaseDocumentModels.Voucher voucherDoc) {
            var baseId = voucherDoc.Id.ToString();
            var ret = new List<VoucherRedeemResponse.VoucherInfo>(voucherDoc.Count);
            for(int i = 0; i < voucherDoc.Count; ++i) {
                ret.Add(new VoucherRedeemResponse.VoucherInfo {
                    Id = string.Format("{0}/{1}", baseId, i+1),
                    Aim = voucherDoc.AimCode,
                    Latitude = voucherDoc.Position.Coordinates.Latitude,
                    Longitude = voucherDoc.Position.Coordinates.Longitude,
                    Timestamp = voucherDoc.Timestamp,
                    Secret = voucherDoc.Secret
                });
            }
            return ret;
        }

        /// <summary>
        /// Confirms a voucher generation request and redeems vouchers.
        /// </summary>
        /// <param name="payload">Voucher redemption payload.</param>
        [HttpPost("redeem")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Redeem(
            [FromBody] VoucherRedeemPayload payload
        ) {
            if(payload == null) {
                return BadRequest();
            }

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

                var (source, vouchers) = await Operator.GenerateVouchers(payloadContent.Otc, payloadContent.Password);

                Logger.LogInformation("Successfully redeemed vouchers by source {0}", source.Id);

                var inflatedVouchers = vouchers.SelectMany(v => InflateVouchers(v));
                var content = new VoucherRedeemResponse.Content {
                    SourceId = new Identifier(source.Id.ToString()),
                    SourceName = source.Name,
                    Vouchers = inflatedVouchers.ToArray()
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
