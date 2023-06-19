using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api.InputModels {
    public class AddressInformation {
        [StringLength(256)]
        public string StreetName { get; set; }

        [StringLength(16)]
        public string StreetNumber { get; set; }

        [StringLength(16)]
        public string ZipCode { get; set; }

        [StringLength(128)]
        public string City { get; set; }

        [StringLength(64)]
        public string Country { get; set; }

        [StringLength(1024)]
        public string FormattedAddress { get; set; }

        public string GoogleMapsPlaceId { get; set; }
    }
}
