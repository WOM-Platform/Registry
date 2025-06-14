using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.InputModels.Pos {
    public class PosRegistrationInput {
        [Required]
        public ObjectId OwnerMerchantId { get; set; }

        [Required]
        [StringLength(128, MinimumLength = 4)]
        public string Name { get; set; }

        [StringLength(4096)]
        public string? Description { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public AddressInformation? Address { get; set; }

        [Url]
        public string? Url { get; set; }

        public bool? IsActive { get; set; }
    }
}
