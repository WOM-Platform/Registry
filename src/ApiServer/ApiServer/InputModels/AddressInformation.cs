using System.ComponentModel.DataAnnotations;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.InputModels {
    public class AddressInformation {
        [StringLength(256)]
        public string? StreetName { get; set; }

        [StringLength(16)]
        public string? StreetNumber { get; set; }

        [StringLength(16)]
        public string? ZipCode { get; set; }

        [StringLength(128)]
        public string? City { get; set; }

        [StringLength(64)]
        public string? Country { get; set; }

        [StringLength(1024)]
        public string? FormattedAddress { get; set; }

        public string? GoogleMapsPlaceId { get; set; }
    }

    public static class AddressInformationExtensions {
        public static AddressBlock ToDocument(this AddressInformation addressInformation) {
            return addressInformation == null ? null : new AddressBlock {
                StreetName = addressInformation.StreetName,
                StreetNumber = addressInformation.StreetNumber,
                ZipCode = addressInformation.ZipCode,
                City = addressInformation.City,
                Country = addressInformation.Country,
                FormattedAddress = addressInformation.FormattedAddress,
                GoogleMapsPlaceId = addressInformation.GoogleMapsPlaceId,
            };
        }
    }
}
