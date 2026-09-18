using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels.Campaign {
    public class UpdateCampaignSubscriberInput {
        [Required]
        public bool IsRevoked { get; init; }
    }
}
