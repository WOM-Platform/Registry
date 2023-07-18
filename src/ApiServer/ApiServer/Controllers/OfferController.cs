using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Offers;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/offer")]
    [OperationsTags("Offers")]
    [RequireHttpsInProd]
    public class OfferController : BaseRegistryController {

        public OfferController(
            IServiceProvider serviceProvider,
            ILogger<OfferController> logger)
        : base(serviceProvider, logger) {

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
            double latitude, double longitude, double range = 10, OfferService.OfferOrder orderBy = OfferService.OfferOrder.Distance
        ) {
            if(range > 30) {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Search range cannot be greater than 30 kms");
            }

            Logger.LogInformation("Searching for POS offers at {0} kms from ({1},{2})", range, latitude, longitude);

            var results = await OfferService.GetOffersWithDistance(latitude, longitude, range, orderBy);

            return Ok(results.Select(go => new PosWithOffersOutput {
                Id = go.Id,
                Name = go.Name,
                Description = go.Description,
                Cover = PicturesService.GetPosCoverOutput(go.CoverPath, go.CoverBlurHash),
                Url = go.Url,
                Position = go.Position.ToOutput(),
                Offers = go.Offers.Select(o => new PosWithOffersOutput.OfferOutput {
                    Id = o.Id,
                    Title = o.Title,
                    Description = o.Description,
                    Payment = new PosWithOffersOutput.OfferOutput.PaymentDetails {
                        RegistryUrl = $"https://{SelfHostDomain}",
                        Otc = o.Payment.Otc,
                        Password = o.Payment.Password,
                        Link = $"https://{SelfLinkDomain}/payment/{o.Payment.Otc:D}",
                    },
                    Cost = o.Payment.Cost,
                    Filter = o.Payment.Filter.ToOutput(),
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

            var results = await OfferService.GetOffersInBox(llx, lly, urx, ury);

            return Ok(results.Select(go => new PosWithOffersOutput {
                Id = go.Id,
                Name = go.Name,
                Description = go.Description,
                Cover = PicturesService.GetPosCoverOutput(go.CoverPath, go.CoverBlurHash),
                Url = go.Url,
                Position = go.Position.ToOutput(),
                Offers = go.Offers.Select(o => new PosWithOffersOutput.OfferOutput {
                    Id = o.Id,
                    Title = o.Title,
                    Description = o.Description,
                    Payment = new PosWithOffersOutput.OfferOutput.PaymentDetails {
                        RegistryUrl = $"https://{SelfHostDomain}",
                        Otc = o.Payment.Otc,
                        Password = o.Payment.Password,
                        Link = $"https://{SelfLinkDomain}/payment/{o.Payment.Otc:D}",
                    },
                    Cost = o.Payment.Cost,
                    Filter = o.Payment.Filter.ToOutput(),
                    CreatedOn = o.CreatedOn,
                    LastUpdate = o.LastUpdate,
                }).ToArray(),
            }));
        }

    }

}
