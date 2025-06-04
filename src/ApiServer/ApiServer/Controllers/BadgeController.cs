using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels;
using WomPlatform.Web.Api.InputModels.Badge;
using WomPlatform.Web.Api.OutputModels.Badge;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {
    [Route("v1/badge")]
    [OperationsTags("Badge")]
    [RequireHttpsInProd]
    public class BadgeController : BaseRegistryController {
        public BadgeController(
            IServiceProvider serviceProvider,
            ILogger<BaseRegistryController> logger
        )
            : base(serviceProvider, logger) {
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BadgeOutput[]), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetBadges(
            [FromQuery] VisibilityFilter? visibility
        ) {
            var badges = await BadgeService.GetBadges(
                isPublic: visibility switch {
                    VisibilityFilter.Private => false,
                    VisibilityFilter.Public => true,
                    _ => null,
                }
            );

            return Ok(from badge in badges
                      select badge.ToOutput(PicturesService));
        }

        [HttpGet("{badgeId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BadgeOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetBadge(
            [FromRoute] ObjectId badgeId
        ) {
            var badge = await BadgeService.GetBadgeById(badgeId);
            if(badge == null) {
                return NotFound();
            }

            return Ok(badge.ToOutput(PicturesService));
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(BadgeOutput), StatusCodes.Status201Created)]
        public async Task<IActionResult> RegisterBadge(
            [FromBody] RegisterBadgeInput input,
            [FromServices] IOptions<ApiBehaviorOptions> apiBehaviorOptions
        ) {
            await VerifyUserIsAdmin();

            ObjectId? challengeId = null;
            if(input.ChallengeId != null) {
                var challenge = await BadgeService.GetBadgeChallengeById(ObjectId.Parse(input.ChallengeId));
                if(challenge == null) {
                    ModelState.AddModelError(nameof(input.ChallengeId), "Challenge does not exist");
                    return apiBehaviorOptions.Value.InvalidModelStateResponseFactory(ControllerContext);
                }

                if(challenge.IsPublic != input.IsPublic) {
                    ModelState.AddModelError(nameof(input.IsPublic), "Badge must share same isPublic setting as challenge");
                    return apiBehaviorOptions.Value.InvalidModelStateResponseFactory(ControllerContext);
                }

                challengeId = challenge.Id;
            }

            try {
                var badge = new Badge {
                    ChallengeId = challengeId,
                    IsPublic = input.IsPublic,
                    Name = input.Name,
                    Description = input.Description,
                    InformationUrl = input.InformationUrl,
                    CreatedAt = DateTime.UtcNow,
                };
                await BadgeService.RegisterBadge(badge);

                return Created(
                    Url.Action(nameof(GetBadge), new { badgeId = badge.Id }),
                    badge.ToOutput(PicturesService)
                );
            }
            catch(Exception) {
                Logger.LogError("Failed to register new badge");
                throw;
            }
        }

        [HttpDelete("{badgeId}")]
        [Authorize]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteBadge(
            [FromRoute] ObjectId badgeId
        ) {
            await VerifyUserIsAdmin();

            var success = await BadgeService.DeleteBadge(badgeId);
            if(!success) {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPut("{badgeId}")]
        [Authorize]
        [ProducesResponseType(typeof(BadgeOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReplaceBadge(
            [FromRoute] ObjectId badgeId,
            [FromBody] RegisterBadgeInput input,
            [FromServices] IOptions<ApiBehaviorOptions> apiBehaviorOptions
        ) {
            await VerifyUserIsAdmin();

            var existingBadge = await BadgeService.GetBadgeById(badgeId);
            if(existingBadge == null) {
                return NotFound();
            }

            ObjectId? challengeId = null;
            if(input.ChallengeId != null) {
                var challenge = await BadgeService.GetBadgeChallengeById(ObjectId.Parse(input.ChallengeId));
                if(challenge == null) {
                    ModelState.AddModelError(nameof(input.ChallengeId), "Challenge does not exist");
                    return apiBehaviorOptions.Value.InvalidModelStateResponseFactory(ControllerContext);
                }

                if(challenge.IsPublic != input.IsPublic) {
                    ModelState.AddModelError(nameof(input.IsPublic), "Badge must share same isPublic setting as challenge");
                    return apiBehaviorOptions.Value.InvalidModelStateResponseFactory(ControllerContext);
                }

                challengeId = challenge.Id;
            }

            existingBadge.ChallengeId = challengeId;
            existingBadge.IsPublic = input.IsPublic;
            existingBadge.Name = input.Name;
            existingBadge.Description = input.Description;
            existingBadge.InformationUrl = input.InformationUrl;

            if(!await BadgeService.ReplaceBadge(existingBadge)) {
                return this.UnexpectedError();
            }

            return Ok(existingBadge.ToOutput(PicturesService));
        }

        [HttpPut("{badgeId}/image")]
        [Authorize]
        [DisableRequestSizeLimit]
        [Consumes(MediaTypeNames.Image.Jpeg)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SetBadgeImage(
            [FromRoute] ObjectId badgeId,
            [FromBody] Stream image
        ) {
            await VerifyUserIsAdmin();

            // Safety checks on uploaded file
            if(image == null || image.Length == 0) {
                Logger.LogError("Image null or empty");
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Image null or empty");
            }
            if(image.Length > 4 * 1024 * 1024) {
                Logger.LogError("Image too large ({0} bytes)", image.Length);
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Image too large");
            }

            (var picturePath, var pictureBlurHash) = await PicturesService.ProcessAndUploadPicture(image, badgeId.ToString(), PicturesService.PictureUsage.BadgeImage);

            await BadgeService.UpdateBadgeImage(badgeId, picturePath, pictureBlurHash);

            return Accepted();
        }
    }

}
