using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Connector;
using WomPlatform.Web.Api.OutputModels;
using WomPlatform.Web.Api.OutputModels.Offers;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {

    [Route("v1/offer")]
    [OperationsTags("Offers")]
    public class OfferController : BaseRegistryController {

        private readonly OfferService _offerService;

        public OfferController(
            IConfiguration configuration,
            CryptoProvider crypto,
            KeyManager keyManager,
            OfferService offerService,
            ILogger<MigrationController> logger)
        : base(configuration, crypto, keyManager, logger) {
            _offerService = offerService;
        }

        [HttpPost("search/distance")]
        [AllowAnonymous]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Paged<OfferSearchPosOutput>), StatusCodes.Status200OK)]
        public ActionResult SearchByDistance(
            double latitude, double longitude, double range
        ) {
            return Ok(Paged<OfferSearchPosOutput>.FromAll(new OfferSearchPosOutput[] {
                new OfferSearchPosOutput {
                    PosId = "6295f82084bf4e0021b07ea3",
                    Name = "POS vicino",
                    Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.",
                    Picture = null,
                    Url = "https://example.org",
                    Position = new OutputModels.GeoCoords {
                        Latitude = 42.934872,
                        Longitude = 12.609390
                    },
                    DistanceInKms = 123.45,
                    Offers = new OfferSearchOfferOutput[] {
                        new OfferSearchOfferOutput {
                            OfferId = new Guid("6c7f4b71-fb04-4540-8f76-b3a2e792dfc9"),
                            Title = "Offer 1",
                            Description = "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
                            Picture = null,
                            Cost = 50,
                            CreatedAt = new DateTime(2022, 12, 11, 10, 11, 12),
                            UpdatedAt = new DateTime(2022, 12, 31, 10, 11, 12),
                        },
                        new OfferSearchOfferOutput {
                            OfferId = new Guid("6c7f4b71-fb04-4540-8f76-b3a2e792dfd9"),
                            Title = "Offer 2",
                            Description = "Offendit eleifend moderatius ex vix, quem odio mazim et qui, purto expetendis cotidieque quo cu, veri persius vituperata ei nec.",
                            Picture = null,
                            Cost = 100,
                            CreatedAt = new DateTime(2022, 12, 13, 12, 13, 14),
                            UpdatedAt = new DateTime(2022, 12, 28, 12, 13, 14),
                        }
                    }
                },
                new OfferSearchPosOutput {
                    PosId = "62976d4384bf4e0021b0a4d4",
                    Name = "POS lontano",
                    Description = "Then God said, Let the earth and no herb of the field and every bird of the air, and brought her to the man. God set them in the dome Sky.",
                    Picture = null,
                    Url = "https://example.org",
                    Position = new OutputModels.GeoCoords {
                        Latitude = 42.945000,
                        Longitude = 12.702040
                    },
                    DistanceInKms = 234.56,
                    Offers = new OfferSearchOfferOutput[] {
                        new OfferSearchOfferOutput {
                            OfferId = new Guid("e31217dd-a6f6-4908-b67a-13daa864a2fe"),
                            Title = "Offer 3",
                            Description = "Then God said, Let there be lights in the dome of the waters, and let it separate the day from the night; and let birds multiply on the seventh day and hallowed it.",
                            Picture = null,
                            Cost = 75,
                            CreatedAt = new DateTime(2022, 12, 11, 10, 11, 12),
                            UpdatedAt = new DateTime(2022, 12, 31, 10, 11, 12),
                        }
                    }
                },
            }));
        }

    }

}
