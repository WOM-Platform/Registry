using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels.Merchant {
    public class CreateMerchantInput {
        [Required]
        [MinLength(8)]
        public string Name { get; set; }

        [Required]
        [MinLength(11)] // Length of Partita IVA
        [MaxLength(16)] // Length of Codice Fiscale
        public string FiscalCode { get; set; }

        [Required]
        public MerchantActivityType PrimaryActivity { get; set; }

        [Required]
        public string Address { get; set; }

        public string StreetNumber { get; set; }

        [Required]
        public string ZipCode { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string Country { get; set; }

        public string FormattedAddress { get; set; }

        public string GoogleMapsPlaceId { get; set; }

        public string Description { get; set; }

        [Url]
        public string Url { get; set; }

        public string ActivationCode { get; set; }
    }
}
