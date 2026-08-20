using System;
using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels.Campaign {
    public class RegisterCampaignInput {
        [Required]
        public bool IsPublic { get; init; }

        [Required]
        public string Name { get; init; }

        public CampaignSimpleFilterInput? SimpleFilter { get; init; }

        public string? ImagePath { get; init; }

        public string? Description { get; init; }

        [Url]
        [MaxLength(2048)]
        public string? InformationUrl { get; init; }

        [Required]
        public DateTime StartDate { get; init; }

        public DateTime? EndDate { get; init; }
    }
}
