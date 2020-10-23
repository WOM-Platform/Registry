using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.Controllers {

    [Authorize(Policy = Startup.UserLoginPolicy)]
    [Route("dashboard/merchant")]
    public class DashboardMerchantController : Controller {

        private readonly MongoDatabase _mongo;
        private readonly ILogger<DashboardMerchantController> _logger;

        public DashboardMerchantController(
            MongoDatabase mongo,
            ILogger<DashboardMerchantController> logger
        ) {
            _mongo = mongo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index() {
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "???";

            var merchantId = new ObjectId(User.FindFirst(Startup.ActiveMerchantClaimType).Value);
            _logger.LogDebug("Active merchant: {0}", merchantId);

            var merchant = await _mongo.GetMerchantById(merchantId);
            var posList = await _mongo.GetPosByMerchant(merchantId);

            return View("Home", new MerchantDashboardHomeViewModel {
                UserFullname = username,
                MerchantName = merchant.Name,
                Pos = from p in posList
                      select new MerchantDashboardHomeViewModel.PosViewModel {
                          Id = p.Id.ToString(),
                          Name = p.Name
                      }
            });
        }

        [HttpGet("add-pos")]
        public IActionResult AddNewPos() {
            return View("AddPos");
        }

        [HttpPost("add-pos")]
        public async Task<IActionResult> AddNewPosPerform(
            [FromForm] UserRegisterPosModel input
        ) {
            if(!ModelState.IsValid) {
                return View("AddPos");
            }

            var merchantId = new ObjectId(User.FindFirst(Startup.ActiveMerchantClaimType).Value);

            var keyPair = CryptoHelper.CreateKeyPair();
            await _mongo.CreatePos(new Pos {
                MerchantId = merchantId,
                Name = input.PosName,
                Position = GeoJson.Point(GeoJson.Geographic(input.PosLongitude, input.PosLatitude)),
                PrivateKey = keyPair.Private.ToPemString(),
                PublicKey = keyPair.Public.ToPemString(),
                Url = input.PosUrl
            });

            return RedirectToAction(nameof(Index));
        }

    }

}
