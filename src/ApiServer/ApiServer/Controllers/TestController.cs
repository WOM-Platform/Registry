using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.InputModels;
using WomPlatform.Web.Api.InputModels.Generation;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("debug")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class TestController : BaseRegistryController {

        private readonly ObjectId _testSourceId, _testPosId;

        private IWebHostEnvironment Hosting { get; init; }

        public TestController(
            IWebHostEnvironment webHostEnvironment,
            IServiceProvider serviceProvider,
            ILogger<AdminController> logger)
        : base(serviceProvider, logger) {
            Hosting = webHostEnvironment;

            var devSection = Configuration.GetSection("DevelopmentSetup");
            
            var devSourceSection = devSection.GetSection("Source");
            _testSourceId = new ObjectId(devSourceSection["Id"]);

            var devPosSection = devSection.GetSection("Pos");
            _testPosId = new ObjectId(devPosSection["Id"]);
        }

        [HttpPost("create-vouchers/{count=10}")]
        public async Task<IActionResult> CreateVouchers(
            [FromRoute] int count
        ) {
            if(!Hosting.IsDevelopment()) {
                return Unauthorized();
            }

            if(count <= 0 || count > 100) {
                throw new ArgumentOutOfRangeException(nameof(count), "Voucher count out of range");
            }

            Logger.LogInformation("Creating {0} test vouchers", count);

            var testSource = await SourceService.GetSourceById(_testSourceId);
            var rnd = new Random();
            var aim = (await AimService.GetAllAims()).OrderBy(a => rnd.NextDouble()).First();

            Logger.LogTrace("Test source: {0}, random aim '{1}'", testSource.Name, aim.Code);

            var now = DateTime.UtcNow;
            var voucherInfos = new VoucherGenerationSpecification[] {
                new VoucherGenerationSpecification {
                    Aim = aim.Code,
                    Location = new GeoCoordsInput {
                        Latitude = rnd.NextBetween(5, 40),
                        Longitude = rnd.NextBetween(5, 50),
                    },
                    Timestamp = DateTime.UtcNow,
                    Count = count
                }
            };

            (var generation, _) = await GenerationService.CreateGenerationRequest(testSource, voucherInfos, isPreVerified: true);

            Logger.LogDebug("New voucher generation request created with code {0}", generation.Otc);

            return Ok(new {
                OtcGen = UrlGenerator.GenerateRedeemUrl(generation.Otc),
                Pin = generation.Password,
            });
        }

        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment(
            string ackUrl,
            int amount = 10,
            [FromBody] SimpleFilterInput filter = null
        ) {
            if (!Hosting.IsDevelopment()) {
                return Unauthorized();
            }

            if(amount <= 0) {
                throw new ArgumentOutOfRangeException("Amount cannot be zero or negative", nameof(amount));
            }
            if(string.IsNullOrWhiteSpace(ackUrl)) {
                throw new ArgumentException("Acknowledgment URL cannot be void", nameof(ackUrl));
            }

            Logger.LogInformation("Creating payment for {0} vouchers", amount);

            var testPos = await PosService.GetPosById(_testPosId);

            Logger.LogTrace("Test POS: {0}", testPos.Id);

            var request = await PaymentService.CreatePaymentRequest(testPos, amount, filter.ToDocument(), null, null, ackUrl, null, false, true);

            Logger.LogDebug("New payment request created with code {0}", request.Otc);

            return Ok(new {
                OtcPay = UrlGenerator.GeneratePaymentUrl(request.Otc),
                Pin = request.Password
            });
        }

        [HttpGet("payment/{otc:guid}")]
        public async Task<IActionResult> GetPaymentInfo(
            [FromRoute] Guid otc
        ) {
            if(!Hosting.IsDevelopment()) {
                return Unauthorized();
            }

            var payment = await PaymentService.GetPaymentRequestByOtc(otc);
            if(payment == null) {
                return NotFound();
            }

            return Ok(payment);
        }

    }

}
