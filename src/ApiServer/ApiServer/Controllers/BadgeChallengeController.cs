using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels;
using WomPlatform.Web.Api.InputModels.Badge;
using WomPlatform.Web.Api.OutputModels.Badge;

namespace WomPlatform.Web.Api.Controllers {
    [Route("v1/badge/challenge")]
    [OperationsTags("Badge")]
    [RequireHttpsInProd]
    public class BadgeChallengeController : BaseRegistryController {
        public BadgeChallengeController(
            IServiceProvider serviceProvider,
            ILogger<BaseRegistryController> logger
        ) : base(serviceProvider, logger) {
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BadgeChallengeOutput[]), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetBadgeChallenges(
            [FromQuery] VisibilityFilter? visibility
        ) {
            var challenges = await BadgeService.GetBadgeChallenges(
                isPublic: visibility switch {
                    VisibilityFilter.Private => false,
                    VisibilityFilter.Public => true,
                    _ => null,
                }
            );

            return Ok(from challenge in challenges
                      select challenge.ToOutput([], PicturesService));
        }

        [HttpGet("{badgeChallengeId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BadgeChallengeOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetBadgeChallenge(
            [FromRoute] ObjectId badgeChallengeId
        ) {
            var badgeChallenge = await BadgeService.GetBadgeChallengeById(badgeChallengeId);
            if(badgeChallenge == null) {
                return NotFound();
            }

            var badges = await BadgeService.GetBadges(challengeId: badgeChallengeId);

            return Ok(badgeChallenge.ToOutput(badges, PicturesService));
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(BadgeOutput), StatusCodes.Status201Created)]
        public async Task<ActionResult> RegisterBadge(
            [FromBody] RegisterBadgeChallengeInput input
        ) {
            await VerifyUserIsAdmin();

            try {
                var challenge = new BadgeChallenge {
                    IsPublic = input.IsPublic,
                    Name = input.Name,
                    Description = input.Description,
                    InformationUrl = input.InformationUrl,
                    CreatedAt = DateTime.UtcNow,
                };
                await BadgeService.RegisterBadgeChallenge(challenge);

                return Created(
                    Url.Action(nameof(GetBadgeChallenge), new { badgeChallengeId = challenge.Id }),
                    challenge.ToOutput([], PicturesService)
                );
            }
            catch(Exception) {
                Logger.LogError("Failed to register new badge challenge");
                throw;
            }
        }

        [HttpDelete("{badgeChallengeId}")]
        [Authorize]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteBadge(
            [FromRoute] ObjectId badgeChallengeId
        ) {
            await VerifyUserIsAdmin();

            var success = await BadgeService.DeleteBadgeChallenge(badgeChallengeId);
            if(!success) {
                return NotFound();
            }

            return NoContent();
        }
    }
}
