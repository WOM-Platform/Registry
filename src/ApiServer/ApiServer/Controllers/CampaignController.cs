using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels;
using WomPlatform.Web.Api.InputModels.Campaign;
using WomPlatform.Web.Api.OutputModels.Campaign;
using WomPlatform.Web.Api.OutputModels.Pos;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.Controllers {
    [Route("v1/campaign")]
    [OperationsTags("Campaign")]
    [RequireHttpsInProd]
    public class CampaignController : BaseRegistryController {
        public CampaignController(
            IServiceProvider serviceProvider,
            ILogger<BaseRegistryController> logger
        ) : base(serviceProvider, logger) {
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CampaignOutput[]), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetCampaigns(
            [FromQuery] VisibilityFilter? visibility
        ) {
            List<Campaign>? campaigns = await CampaignService.GetCampaigns(
                visibility switch {
                    VisibilityFilter.Private => false,
                    VisibilityFilter.Public => true,
                    _ => null
                }
            );

            return Ok(
                from campaign in campaigns
                let picCover = PicturesService.GetPosCoverOutput(
                    campaign.CoverPath,
                    campaign.CoverBlurHash
                )
                select campaign.ToOutput(picCover)
            );
        }

        [HttpGet("{campaignId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CampaignOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCampaign(
            [FromRoute] ObjectId campaignId
        ) {
            Campaign? campaign = await CampaignService.GetCampaignById(campaignId);
            if(campaign == null) {
                return NotFound();
            }

            var picCover = PicturesService.GetPosCoverOutput(campaign.CoverPath, campaign.CoverBlurHash);

            return Ok(campaign.ToOutput(picCover));
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(CampaignOutput), StatusCodes.Status201Created)]
        public async Task<IActionResult> RegisterCampaign(
            [FromBody] RegisterCampaignInput campaignInput
        ) {
            await VerifyUserIsAdmin();

            try {
                Campaign? campaign = new Campaign {
                    IsPublic = campaignInput.IsPublic,
                    Name = campaignInput.Name,
                    SimpleFilter = campaignInput.SimpleFilter.ToDocument() ?? new CampaignSimpleFilter(),
                    WomCount = 0,
                    Description = campaignInput.Description,
                    InformationUrl = campaignInput.InformationUrl,
                    StartDate = campaignInput.StartDate,
                    EndDate = campaignInput.EndDate,
                    CreatedAt = DateTime.UtcNow
                };

                await CampaignService.RegisterCampaign(campaign);

                return Created(
                    Url.Action(nameof(GetCampaign), new { campaignId = campaign.Id }),
                    campaign.ToOutput(PicturesService.DefaultCampaignCover)
                );
            }
            catch(Exception) {
                Logger.LogError("Failed to register new campaign");
                throw;
            }
        }

        [HttpPut("{campaignId}")]
        [Authorize]
        [ProducesResponseType(typeof(CampaignOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateCampaign(
            [FromRoute] ObjectId campaignId,
            [FromBody] RegisterCampaignInput campaignInput
        ) {
            await VerifyUserIsAdmin();

            if(campaignInput == null) {
                return BadRequest("Input cannot be null.");
            }

            Campaign? campaign = await CampaignService.GetCampaignById(campaignId);
            if(campaign == null) {
                return NotFound();
            }

            campaign.IsPublic = campaignInput.IsPublic;
            campaign.Name = campaignInput.Name;
            campaign.SimpleFilter = campaignInput.SimpleFilter.ToDocument() ?? new CampaignSimpleFilter();
            campaign.Description = campaignInput.Description;
            campaign.InformationUrl = campaignInput.InformationUrl;
            campaign.StartDate = campaignInput.StartDate;
            campaign.EndDate = campaignInput.EndDate;
            campaign.LastUpdate = DateTime.UtcNow;

            bool success = await CampaignService.ReplaceCampaign(campaign);

            if(!success) {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to update the campaign."
                );
            }
            var picPosCover = PicturesService.GetCampaignCoverOutput(campaign.CoverPath, campaign.CoverBlurHash);

            return Ok(campaign.ToOutput(picPosCover));
        }

         /// <summary>
        /// Updates the cover of an existing campaign.
        /// </summary>
        [HttpPost("{campaignId}/cover")]
        [HttpPut("{campaignId}/cover")]
        [Authorize]
        [DisableRequestSizeLimit]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(CampaignOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateCover(
            [FromRoute] ObjectId campaignId,
            [Required] IFormFile image
        ) {
             await VerifyUserIsAdmin();

             Campaign? campaign = await CampaignService.GetCampaignById(campaignId);
             if(campaign == null) {
                 return NotFound();
             }

            // Safety checks on uploaded file
            if(image == null || image.Length == 0) {
                Logger.LogError("Image field null or empty");
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Image field null or empty");
            }
            if(image.Length > 4 * 1024 * 1024) {
                Logger.LogError("Image too large ({0} bytes)", image.Length);
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Image too large");
            }

            try {
                var campaignUrl = campaign.Name.ToCleanUrl();

                // Process and upload image
                using var stream = new MemoryStream();
                await image.CopyToAsync(stream);
                (var picturePath, var pictureBlurHash) = await PicturesService.ProcessAndUploadPicture(stream, campaignUrl, PicturesService.PictureUsage.CampaignCover);

                await CampaignService.UpdateCampaignCover(campaignId, picturePath, pictureBlurHash);

                var picPosCover = PicturesService.GetCampaignCoverOutput(picturePath, pictureBlurHash);

                return Ok(campaign.ToOutput(picPosCover));
            }
            catch(Exception) {
                Logger.LogError("Failed to update POS {0}", campaignId);
                throw;
            }
        }

        [HttpDelete("{campaignId}")]
        [Authorize]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCampaign(
            [FromRoute] ObjectId campaignId
        ) {
            await VerifyUserIsAdmin();

            bool success = await CampaignService.DeleteCampaign(campaignId);

            if(!success) {
                return NotFound();
            }

            return NoContent();
        }


        [HttpPost("{campaignId}/subscribers")]
        [AllowAnonymous]
        [ProducesResponseType(
            typeof(CampaignSubscriberOutput),
            StatusCodes.Status201Created
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound
        )]
        public async Task<ActionResult> RegisterCampaignSubscriber(
            [FromRoute] ObjectId campaignId
        ) {
            var campaign = await CampaignService.GetCampaignById(campaignId);

            if(campaign == null) {
                return NotFound();
            }

            try {
                var subscriber = await CampaignSubscriberService.RegisterSubscriber(
                    campaignId
                );

                return Created(
                    (string?)null,
                    subscriber.ToOutput()
                );
            }
            catch(Exception) {
                Logger.LogError(
                    "Failed to register subscriber for campaign {CampaignId}",
                    campaignId
                );
                throw;
            }
        }

        [HttpPut("{campaignId}/subscribers/{token}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateCampaignSubscriber(
            [FromRoute] ObjectId campaignId,
            [FromRoute] string token,
            [FromBody] UpdateCampaignSubscriberInput input
        ) {
            await VerifyUserIsAdmin();

            if(input == null) {
                return BadRequest("Input cannot be null.");
            }

            var subscriber = await CampaignSubscriberService.UpdateCampaignSubscriber(
                campaignId,
                token,
                input.IsRevoked
            );

            if(subscriber == null) {
                return NotFound();
            }

            return Ok();
        }

        [HttpGet("{campaignId}/subscribers")]
        [Authorize]
        [ProducesResponseType(typeof(CampaignSubscriberOutput[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetListSubscribers(
            [FromRoute] ObjectId campaignId
        ) {
            await VerifyUserIsAdmin();

            var campaign = await CampaignService.GetCampaignById(campaignId);

            if(campaign == null) {
                return NotFound();
            }

            var subscribers = await CampaignSubscriberService.GetSubscribers(campaignId);

            return Ok(
                from subscriber in subscribers
                select new {
                    token = subscriber.Token,
                    registeredAt = subscriber.RegisteredAt,
                    lastUsedAt = subscriber.LastUsedAt,
                    isRevoked = subscriber.IsRevoked
                }
            );
        }

        [HttpPost("{campaignId}/subscribers/{token}/contributions")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RegisterCampaignContribution(
            [FromRoute] ObjectId campaignId,
            [FromRoute] string token,
            [FromBody] RegisterCampaignContributionInput contributionInput
        ) {
            var campaign = await CampaignService.GetCampaignById(campaignId);

            if(campaign == null) {
                return NotFound();
            }

            var subscriber = await CampaignSubscriberService.GetSubscriber(
                campaignId, token
            );

            if(subscriber == null || subscriber.IsRevoked) {
                return NoContent();
            }
            // check contribution date is not older than 1 day, if it is, return 204 No Content
            if(contributionInput.ContributedAt < DateTime.UtcNow.AddDays(-1)) {
                return BadRequest("Contribution date cannot be older than 1 day.");
            }

            try {
                var contribution = new CampaignContribution {
                    CampaignId = campaignId,
                    Token = token,
                    ContributedAt = contributionInput.ContributedAt,
                    WomCount = contributionInput.WomCount
                };

                await CampaignContributionService.RegisterContribution(
                    contribution
                );

                return StatusCode(StatusCodes.Status201Created);
            }
            catch(Exception) {
                Logger.LogError(
                    "Failed to register contribution for campaign {CampaignId} and token {Token}",
                    campaignId,
                    token
                );
                throw;
            }
        }

        [HttpGet("{campaignId}/contributions")]
        [Authorize]
        [ProducesResponseType(
            typeof(List<CampaignContribution>),
            StatusCodes.Status200OK
        )]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound
        )]
        public async Task<ActionResult> GetCampaignContributions(
            [FromRoute] ObjectId campaignId
        ) {
            var campaign = await CampaignService.GetCampaignById(campaignId);

            if(campaign == null) {
                return NotFound();
            }

            var contributions =
                await CampaignContributionService.GetContributions(campaignId);

            return Ok(contributions);
        }
    }
}
