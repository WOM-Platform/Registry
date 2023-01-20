using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.InputModels.Pos {
    public class PosRegistrationInput {
        [Required]
        public ObjectId OwnerMerchantId { get; set; }

        [Required]
        [StringLength(128, MinimumLength = 4)]
        public string Name { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Url]
        public string Url { get; set; }

        public bool? IsActive { get; set; }
    }
}
