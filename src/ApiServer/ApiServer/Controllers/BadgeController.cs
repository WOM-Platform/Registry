using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels.Badge;
using WomPlatform.Web.Api.OutputModels.Badge;
using WomPlatform.Web.Api.OutputModels.Merchant;
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

            var imageOutput = badge.ImagePath != null ? PicturesService.GetPictureOutput(badge.ImagePath, badge.ImageBlurHash) : null;

            return Ok(badge.ToOutput(imageOutput));
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(BadgeOutput), StatusCodes.Status201Created)]
        public async Task<ActionResult> RegisterBadge(
            [FromBody] RegisterBadgeInput input
        ) {
            await VerifyUserIsAdmin();

            ObjectId? challengeId = null;
            if(input.ChallengeId != null) {
                // TODO: load challenge and verify it exists
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
                    badge.ToOutput(null)
                );
            }
            catch(Exception) {
                Logger.LogError("Failed to register new badge");
                throw;
            }
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
