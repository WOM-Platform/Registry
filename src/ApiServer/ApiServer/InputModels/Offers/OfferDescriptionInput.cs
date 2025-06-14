using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels.Offers {
    public class OfferDescriptionInput {
        [Required]
        [StringLength(64, MinimumLength = 3)]
        public string Title { get; set; }

        [StringLength(1024)]
        public string? Description { get; set; }
    }
}
