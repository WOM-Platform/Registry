using System;
using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels.Campaign {
    public class RegisterCampaignContributionInput {
        [Required]
        [Range(1, int.MaxValue)]
        public int WomCount { get; init; }

        [Required]
        public DateTime ContributedAt { get; init; }
    }
}
