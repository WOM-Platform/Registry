using System;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Connector.Models;
using MongoDB.Bson;
using Org.BouncyCastle.Crypto;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/transfer")]
    [OperationsTags("Transfer", "Operations")]
    public class TransferController : BaseRegistryController {
        public TransferController(
            IServiceProvider serviceProvider,
            ILogger<TransferController> logger)
        : base(serviceProvider, logger) {
        }

        /// <summary>
        /// Registers a new voucher transfer.
        /// </summary>
        [HttpPost("request")]
        [ProducesResponseType(typeof(VoucherTransferResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Request(
            [FromBody] VoucherTransferPayload payload
        ) {
            if(payload == null || payload.Nonce == null) {
                Logger.LogError(LoggingEvents.VoucherTransfer, "Payload or nonce void, aborting");

                return BadRequest();
            }

            var confKnownEntities = Configuration.GetSection("KnownEntities");
            if(!payload.SourceId.Equals(confKnownEntities["ExchangeSourceId"])) {
                Logger.LogError(LoggingEvents.VoucherTransfer, "Received transfer request not using known exchange source ID, aborting");

                return BadRequest();
            }

            Logger.LogInformation(LoggingEvents.VoucherTransfer, "Received voucher transfer request");

            var source = await SourceService.GetSourceById(new ObjectId(payload.SourceId));
            if(source == null) {
                Logger.LogError(LoggingEvents.VoucherTransfer, "Source ID {0} does not exist", payload.SourceId);
                return this.SourceNotFound();
            }
            var sourcePrivateKey = CryptoHelper.LoadKeyFromString<AsymmetricCipherKeyPair>(source.PrivateKey);

            VoucherTransferPayload.Content content;
            try {
                content = Crypto.Decrypt<VoucherTransferPayload.Content>(payload.Payload, sourcePrivateKey.Private);
            }
            catch(Exception ex) {
                Logger.LogError(LoggingEvents.VoucherTransfer, ex, "Failed to decrypt payload");
                throw;
            }

            byte[] ks = content.SessionKey.FromBase64();
            if(ks.Length < 32) {
                Logger.LogError(LoggingEvents.VoucherTransfer, "Insufficient session key length ({0} bytes)", ks.Length);
                return this.ProblemParameter($"Length of {nameof(content.SessionKey)} not valid");
            }

            var voucherCount = await PaymentService.MarkVouchersForTransfer(content);
            Logger.LogDebug("Total amount of {0} vouchers marked as spent for transfer", voucherCount);

            (var generation, _) = await GenerationService.CreateGenerationRequest(source, new InputModels.Generation.VoucherGenerationSpecification[] {
                new InputModels.Generation.VoucherGenerationSpecification {
                    Aim = "XX",
                    Count = voucherCount,
                    CreationMode = DatabaseDocumentModels.VoucherCreationMode.SetLocationOnRedeem,
                    Timestamp = DateTime.UtcNow,
                }
            }, isPreVerified: true);

            return Ok(new VoucherTransferResponse {
                Payload = Crypto.Encrypt(new VoucherTransferResponse.Content {
                    RegistryUrl = $"https://{SelfHostDomain}",
                    Nonce = payload.Nonce,
                    Otc = generation.Otc,
                    Password = generation.Password,
                    Link = $"https://{SelfLinkDomain}/vouchers/{generation.Otc:D}",
                    Count = generation.TotalVoucherCount.Value,
                }, ks),
            });
        }

    }

}
