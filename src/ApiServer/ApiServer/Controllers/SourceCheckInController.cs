using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using WomPlatform.Web.Api.Authentication;
using WomPlatform.Web.Api.InputModels;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {
    [Route("v1/source/{sourceId}/checkin")]
    [OperationsTags("Check-in")]
    [RequireHttpsInProd]
    public class SourceCheckInController : BaseRegistryController {
        CountMeInService _countMeInService;

        public SourceCheckInController(
            CountMeInService countMeInService,
            IServiceProvider serviceProvider,
            ILogger<SourceCheckInController> logger
        ) : base(serviceProvider, logger) {
            _countMeInService = countMeInService;
        }

        /// <summary>
        /// Input for check-in creation.
        /// </summary>
        /// <param name="EventTitle">Mandatory title of the event.</param>
        /// <param name="EventStart">Date and time when the event starts.</param>
        /// <param name="EventEnd">Date and time when the event ends.</param>
        /// <param name="Location">Location details, if no totem is supplied.</param>
        /// <param name="TotemId">Fixed totem ID, if no location details are supplied.</param>
        /// <param name="WomGeneration">WOM generation details.</param>
        public record CreateCheckinInput(
            string EventTitle,
            DateTime EventStart,
            DateTime EventEnd,
            CreateCheckInLocationInput? Location,
            string? TotemId,
            CreateCheckInWomInput WomGeneration
        );

        /// <summary>
        /// Input for check-in location.
        /// </summary>
        /// <param name="Name">Optional name of the location.</param>
        /// <param name="Coords">Mandatory geographical position of the location.</param>
        public record CreateCheckInLocationInput(
            string? Name,
            GeoCoordsInput Coords
        );

        /// <summary>
        /// Input for check-in WOM generation.
        /// </summary>
        /// <param name="Aim">Aim code.</param>
        /// <param name="Count">Amount of WOM to generate for each check-in.</param>
        public record CreateCheckInWomInput(
            string Aim,
            int Count
        );

        /// <summary>
        /// Output for check-in generation.
        /// </summary>
        /// <param name="RegistryUrl">Base URL of this Registry instance.</param>
        /// <param name="EventId">Unique event ID.</param>
        /// <param name="Link">URL to be used as a check-in totem.</param>
        public record CreateCheckInOutput(
            string RegistryUrl,
            string EventId,
            string Link
        );

        [HttpPost]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationSchemeOptions.SchemeName)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(CreateCheckInOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCheckin(
            [FromRoute] ObjectId sourceId,
            [FromBody] CreateCheckinInput input
        ) {
            var apiBehaviorOptions = ServiceProvider.GetRequiredService<IOptions<ApiBehaviorOptions>>();
            if(input.Location == null && input.TotemId == null) {
                ModelState.AddModelError(nameof(input.Location), "Location or totem ID must be set");
                ModelState.AddModelError(nameof(input.TotemId), "Location or totem ID must be set");
                return apiBehaviorOptions.Value.InvalidModelStateResponseFactory(ControllerContext);
            }
            else if(input.Location != null && input.TotemId != null) {
                ModelState.AddModelError(nameof(input.Location), "Location and totem ID cannot be both set");
                ModelState.AddModelError(nameof(input.TotemId), "Location and totem ID cannot be both set");
                return apiBehaviorOptions.Value.InvalidModelStateResponseFactory(ControllerContext);
            }

            var source = await VerifyUserIsAdminOfSource(sourceId);

            var response = await _countMeInService.CreateEvent(source, input);

            return Created(
                Url.Action(nameof(GetCheckin), new { response.EventId }),
                new CreateCheckInOutput(
                    $"https://{SelfHostDomain}",
                    $"{response.EventId:D}",
                    $"https://{SelfLinkDomain}/cmi/{response.ProviderId:D}/{response.TotemId:D}"
                )
            );
        }

        [HttpGet("{eventId}")]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationSchemeOptions.SchemeName)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(CreateCheckInOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCheckin(
            [FromRoute] ObjectId sourceId,
            [FromRoute] string eventId
        ) {
            var source = await VerifyUserIsAdminOfSource(sourceId);

            var totem = await _countMeInService.GetEventById(source, eventId);
            if(totem == null) {
                return NotFound();
            }

            return Ok(new CreateCheckInOutput(
                $"https://{SelfHostDomain}",
                $"{totem.EventId:D}",
                $"https://{SelfLinkDomain}/cmi/{totem.ProviderId:D}/{totem.TotemId:D}"
            ));
        }

        [HttpDelete("{eventId}")]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationSchemeOptions.SchemeName)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteCheckin(
            [FromRoute] ObjectId sourceId,
            [FromRoute] string eventId
        ) {
            var source = await VerifyUserIsAdminOfSource(sourceId);

            var done = await _countMeInService.DeleteEvent(source, eventId);
            if(!done) {
                return NotFound();
            }

            return Ok();
        }

        public record GetTotemsOutput(
            GetTotemsEntryOutput[] Totems
        );

        public record GetTotemsEntryOutput(
            string Id,
            string Name
        );

        [HttpGet("totems")]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationSchemeOptions.SchemeName)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(GetTotemsOutput), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetTotems(
            [FromRoute] ObjectId sourceId
        ) {
            var source = await VerifyUserIsAdminOfSource(sourceId);

            // TODO

            return Ok(new GetTotemsOutput(
                [
                    new GetTotemsEntryOutput(
                        "zsDfb01JGGTevDoynxKw",
                        "Totem fisso A"
                    ),
                    new GetTotemsEntryOutput(
                        "buG0GbKV65y0eO0k1CZg",
                        "Totem fisso B"
                    ),
                    new GetTotemsEntryOutput(
                        "2MmDLmF3Q1I0uFr5TlC6",
                        "Totem fisso C"
                    )
                ]
            ));
        }
    }
}
