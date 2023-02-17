using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Offers;
using WomPlatform.Web.Api.Service;
using static WomPlatform.Web.Api.Service.OfferService;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/offer")]
    [OperationsTags("Offers")]
    public class OfferController : BaseRegistryController {

        private readonly OfferService _offerService;
        private readonly PaymentService _paymentService;
        private readonly PosService _posService;
        private readonly MerchantService _merchantService;
        private readonly PicturesService _picturesService;

        public OfferController(
            OfferService offerService,
            PaymentService paymentService,
            PosService posService,
            MerchantService merchantService,
            PicturesService picturesService,
            IServiceProvider serviceProvider,
            ILogger<AdminController> logger)
        : base(serviceProvider, logger) {
            _offerService = offerService;
            _paymentService = paymentService;
            _posService = posService;
            _merchantService = merchantService;
            _picturesService = picturesService;
        }

        /// <summary>
        /// Searches available points of service and offers around a location.
        /// </summary>
        /// <param name="latitude">Latitude of the location.</param>
        /// <param name="longitude">Longitude of the location.</param>
        /// <param name="range">Maximum range to search, in kilometers. Defaults to 10 kms.</param>
        [HttpPost("search/distance")]
        [AllowAnonymous]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(PosWithOffersOutput[]), StatusCodes.Status200OK)]
        public async Task<ActionResult> SearchByDistance(
            double latitude, double longitude, double range = 10, OfferOrder orderBy = OfferOrder.Distance
        ) {
            if(range > 30) {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Search range cannot be greater than 30 kms");
            }

            Logger.LogInformation("Searching for POS offers at {0} kms from ({1},{2})", range, latitude, longitude);

            var results = await _offerService.GetOffersWithDistance(latitude, longitude, range, orderBy);

            return Ok(results.Select(go => new PosWithOffersOutput {
                Id = go.Id,
                Name = go.Name,
                Description = go.Description,
                Cover = (go.CoverPath != null) ? _picturesService.GetPictureOutput(go.CoverPath, go.CoverBlurHash) : _picturesService.DefaultPosCover,
                Url = go.Url,
                Position = go.Position.ToOutput(),
                Offers = go.Offers.Select(o => new PosWithOffersOutput.OfferOutput {
                    Id = o.Id,
                    Title = o.Title,
                    Description = o.Description,
                    Cost = o.Cost,
                    Filter = o.Filter.ToOutput(),
                    CreatedOn = o.CreatedOn,
                    LastUpdate = o.LastUpdate,
                }).ToArray(),
            }));
        }

        [HttpPost("search/box")]
        [AllowAnonymous]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(PosWithOffersOutput[]), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchByBox(
            [FromQuery] double llx, [FromQuery] double lly,
            [FromQuery] double urx, [FromQuery] double ury
        ) {
            Logger.LogInformation("Searching for POS offers between ({0},{1}) and ({2},{3})", llx, lly, urx, ury);

            var results = await _offerService.GetOffersInBox(llx, lly, urx, ury);

            return Ok(results.Select(go => new PosWithOffersOutput {
                Id = go.Id,
                Name = go.Name,
                Description = go.Description,
                Cover = (go.CoverPath != null) ? _picturesService.GetPictureOutput(go.CoverPath, go.CoverBlurHash) : _picturesService.DefaultPosCover,
                Url = go.Url,
                Position = go.Position.ToOutput(),
                Offers = go.Offers.Select(o => new PosWithOffersOutput.OfferOutput {
                    Id = o.Id,
                    Title = o.Title,
                    Description = o.Description,
                    Cost = o.Cost,
                    Filter = o.Filter.ToOutput(),
                    CreatedOn = o.CreatedOn,
                    LastUpdate = o.LastUpdate,
                }).ToArray(),
            }));
        }

        [HttpPost("migrate")]
        [Authorize]
        public async Task<ActionResult> Migrate() {
            if(!await VerifyUserIsAdmin()) {
                return Forbid();
            }

            Logger.LogInformation("Migrating payments to offers");

            Dictionary<ObjectId, Pos> posMap = new ();
            Dictionary<ObjectId, Merchant> merchantMap = new();

            var payments = await _paymentService.GetPersistentPayments();
            foreach(var payment in payments) {
                if(!posMap.ContainsKey(payment.PosId)) {
                    Logger.LogDebug("Fetching info for POS {0}", payment.PosId);
                    posMap.Add(payment.PosId, await _posService.GetPosById(payment.PosId));
                }
                var pos = posMap[payment.PosId];
                if(pos == null) {
                    Logger.LogWarning("Skipping payment {0} with no POS", payment.Otc);
                    continue;
                }
                if(pos.IsDummy) {
                    continue;
                }

                if(!merchantMap.ContainsKey(pos.MerchantId)) {
                    Logger.LogDebug("Fetcing info for merchant {0}", pos.MerchantId);
                    merchantMap.Add(pos.MerchantId, await _merchantService.GetMerchantById(pos.MerchantId));
                }
                var merchant = merchantMap[pos.MerchantId];
                if(merchant == null) {
                    Logger.LogWarning("Skipping payment {0} with no merchant", payment.Otc);
                    continue;
                }
                if(merchant.IsDummy) {
                    continue;
                }

                var offer = new Offer {
                    Title = "Offerta",
                    Description = null,
                    PaymentRequestId = payment.Otc,
                    Cost = payment.Amount,
                    Filter = payment.Filter,
                    Pos = new Offer.PosInformation {
                        Id = pos.Id,
                        Name = pos.Name,
                        Description = null,
                        CoverPath = null, // TODO
                        CoverBlurHash = null, // TODO
                        Position = pos.Position,
                        Url = pos.Url,
                    },
                    Merchant = new Offer.MerchantInformation {
                        Id = pos.MerchantId,
                        Name = merchant.Name,
                        WebsiteUrl = merchant.WebsiteUrl,
                    },
                    CreatedOn = payment.CreatedAt,
                    LastUpdate = DateTime.UtcNow,
                    Deactivated = false,
                };
                await _offerService.AddOffer(offer);

                Logger.LogInformation("Migrated payment {0} as offer {1}", payment.Otc, offer.Id);
            }

            Logger.LogInformation("{0} payments processed", payments.Count);

            return Ok();
        }

    }

}
