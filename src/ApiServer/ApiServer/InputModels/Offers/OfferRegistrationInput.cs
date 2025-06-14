using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels.Offers {
    public class OfferRegistrationInput {
        [Required]
        [StringLength(64, MinimumLength = 3)]
        public string Title { get; set; }

        [StringLength(1024)]
        public string? Description { get; set; }

        [Required]
        [Range(1, 10000)]
        public int Cost { get; set; }

        public SimpleFilterInput? Filter { get; set; }
    }
}
