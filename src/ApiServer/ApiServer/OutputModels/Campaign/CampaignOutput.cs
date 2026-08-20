using System;
using System.Text.Json.Serialization;
using WomPlatform.Web.Api.Service;

namespace WomPlatform.Web.Api.OutputModels.Campaign {
    public class CampaignOutput {
        public string Id { get; init; }

        public bool IsPublic { get; init; }

        public string Name { get; init; }

        public CampaignSimpleFilterOutput? SimpleFilter { get; init; }

        public int WomCount { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PictureOutput? Image { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? InformationUrl { get; init; }

        public DateTime StartDate { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? EndDate { get; init; }

        public DateTime CreatedAt { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? LastUpdate { get; init; }
    }

    public static class CampaignOutputExtensions {
        public static CampaignOutput ToOutput(
            this DatabaseDocumentModels.Campaign campaign,
            PicturesService pictureService
        ) {
            return new CampaignOutput {
                Id = campaign.Id.ToString(),
                IsPublic = campaign.IsPublic,
                Name = campaign.Name,
                SimpleFilter = campaign.SimpleFilter.ToOutput(),
                WomCount = campaign.WomCount,
                Image = pictureService.GetPictureOutput(
                    campaign.ImagePath,
                    null
                ),
                Description = campaign.Description,
                InformationUrl = campaign.InformationUrl,
                StartDate = campaign.StartDate,
                EndDate = campaign.EndDate,
                CreatedAt = campaign.CreatedAt,
                LastUpdate = campaign.LastUpdate
            };
        }
    }
}
