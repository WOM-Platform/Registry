using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Connector;
using WomPlatform.Connector.Models;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Controllers {

    [ApiVersionNeutral]
    [Route("debug")]
    [RequireHttps]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class TestController : BaseRegistryController {

        private readonly ObjectId _testSourceId, _testPosId;

        private readonly DataContext Database;

        public TestController(
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            DataContext database,
            ILogger<AimsController> logger)
        : base(configuration, crypto, keyManager, mongo, @operator, logger) {
            Hosting = webHostEnvironment;
            Database = database;

            var devSection = configuration.GetSection("DevelopmentSetup");
            
            var devSourceSection = devSection.GetSection("Source");
            _testSourceId = new ObjectId(devSourceSection["Id"]);

            var devPosSection = devSection.GetSection("Pos");
            _testPosId = new ObjectId(devPosSection["Id"]);
        }

        protected IWebHostEnvironment Hosting { get; }

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

            var testSource = await Mongo.GetSourceById(_testSourceId);
            var rnd = new Random();
            var aim = (await Mongo.GetAims()).OrderBy(a => rnd.NextDouble()).First();

            Logger.LogTrace("Test source: {0}, random aim '{1}'", testSource.Name, aim.Code);

            var now = DateTime.UtcNow;
            var voucherInfos = new VoucherCreatePayload.VoucherInfo[] {
                new VoucherCreatePayload.VoucherInfo {
                    Aim = aim.Code,
                    Latitude = rnd.NextBetween(5, 40),
                    Longitude = rnd.NextBetween(5, 50),
                    Timestamp = DateTime.UtcNow,
                    Count = count
                }
            };

            (var otcGen, var pwd, _) = await Operator.CreateGenerationRequest(testSource, new VoucherCreatePayload.Content {
                Nonce = Guid.NewGuid().ToString("N"),
                SourceId = testSource.Id.ToString(),
                Vouchers = voucherInfos
            }, isPreVerified: true);

            Logger.LogDebug("New voucher generation request created with code {0}", otcGen);

            return Ok(new {
                OtcGen = UrlGenerator.GenerateRedeemUrl(otcGen),
                Pin = pwd
            });
        }

        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment(
            string ackUrl,
            int amount = 10,
            [FromBody] SimpleFilter filter = null
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

            var testPos = await Mongo.GetPosById(_testPosId);

            Logger.LogTrace("Test POS: {0}", testPos.Id);

            (var otcPay, var pwd) = await Operator.CreatePaymentRequest(testPos, new PaymentRegisterPayload.Content {
                Amount = amount,
                Nonce = Guid.NewGuid().ToString("N"),
                SimpleFilter = filter,
                PosId = testPos.Id.ToString(),
                PocketAckUrl = ackUrl
            }, isPreVerified: true);

            Logger.LogDebug("New payment request created with code {0}", otcPay);

            return Ok(new {
                OtcPay = UrlGenerator.GeneratePaymentUrl(otcPay),
                Pin = pwd
            });
        }

        [HttpPost("migrate-vouchers")]
        public async Task<IActionResult> MigrateVouchers(
        ) {
            if(!Hosting.IsDevelopment()) {
                return Unauthorized();
            }

            Logger.LogInformation("Starting voucher migration");

            var vouchers = (from v in Database.Vouchers
                            select v).ToList();

            Logger.LogInformation("Loaded {0} vouchers", vouchers.Count);

            const int chunkSize = 1000;
            List<LegacyVoucher> voucherDocuments = new();
            for(int i = 0; i < vouchers.Count; ++i) {
                var v = vouchers[i];

                voucherDocuments.Add(new LegacyVoucher {
                    Id = v.Id,
                    Secret = v.Secret.ToBase64(),
                    AimCode = v.AimCode,
                    Position = GeoJson.Point(GeoJson.Geographic(v.Longitude, v.Latitude)),
                    Timestamp = v.Timestamp,
                    Spent = v.Spent
                });

                if(voucherDocuments.Count >= chunkSize) {
                    Logger.LogDebug("Dumping batch to Mongo at {0}", i+1);

                    await Mongo.AddLegacyVouchers(voucherDocuments);
                    voucherDocuments = new List<LegacyVoucher>();
                }
            }

            return Ok();
        }

    }

}
