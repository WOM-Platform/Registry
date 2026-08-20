using System;
using System.Collections.Generic;
using System.Linq;
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
                select campaign.ToOutput(PicturesService)
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

            return Ok(campaign.ToOutput(PicturesService));
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(CampaignOutput), StatusCodes.Status201Created)]
        public async Task<IActionResult> RegisterCampaign(
            [FromBody] RegisterCampaignInput input
        ) {
            await VerifyUserIsAdmin();

            try {
                Campaign? campaign = new Campaign {
                    IsPublic = input.IsPublic,
                    Name = input.Name,
                    SimpleFilter = input.SimpleFilter.ToDocument() ?? new CampaignSimpleFilter(),
                    WomCount = 0,
                    ImagePath = input.ImagePath,
                    Description = input.Description,
                    InformationUrl = input.InformationUrl,
                    StartDate = input.StartDate,
                    EndDate = input.EndDate,
                    CreatedAt = DateTime.UtcNow
                };

                await CampaignService.RegisterCampaign(campaign);

                return Created(
                    Url.Action(nameof(GetCampaign), new { campaignId = campaign.Id }),
                    campaign.ToOutput(PicturesService)
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
            [FromBody] RegisterCampaignInput input
        ) {
            await VerifyUserIsAdmin();

            if(input == null) {
                return BadRequest("Input cannot be null.");
            }

            Campaign? campaign = await CampaignService.GetCampaignById(campaignId);
            if(campaign == null) {
                return NotFound();
            }

            campaign.IsPublic = input.IsPublic;
            campaign.Name = input.Name;
            campaign.SimpleFilter = input.SimpleFilter.ToDocument();
            campaign.ImagePath = input.ImagePath;
            campaign.Description = input.Description;
            campaign.InformationUrl = input.InformationUrl;
            campaign.StartDate = input.StartDate;
            campaign.EndDate = input.EndDate;
            campaign.LastUpdate = DateTime.UtcNow;

            bool success = await CampaignService.ReplaceCampaign(campaign);

            if(!success) {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to update the campaign."
                );
            }

            return Ok(campaign.ToOutput(PicturesService));
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
    }
}
