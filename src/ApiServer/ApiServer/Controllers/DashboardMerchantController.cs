using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using WomPlatform.Connector;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels;
using WomPlatform.Web.Api.ViewModel;

namespace WomPlatform.Web.Api.Controllers {

    [Authorize(Policy = Startup.UserLoginPolicy)]
    [Route("dashboard/merchant")]
    public class DashboardMerchantController : BaseRegistryController {

        public DashboardMerchantController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            MongoDatabase mongo,
            Operator @operator,
            ILogger<DashboardMerchantController> logger
        ) : base(configuration, crypto, keyManager, mongo, @operator, logger) {

        }

        [HttpGet]
        public async Task<IActionResult> Index() {
            var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "???";

            var merchantId = new ObjectId(User.FindFirst(Startup.ActiveMerchantClaimType).Value);
            Logger.LogDebug("Active merchant: {0}", merchantId);

            var merchant = await Mongo.GetMerchantById(merchantId);
            var posList = await Mongo.GetPosByMerchant(merchantId);

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

            await CreatePos(merchantId, input.PosName, input.PosUrl, input.PosLatitude, input.PosLongitude);

            return RedirectToAction(nameof(Index));
        }

    }

}
