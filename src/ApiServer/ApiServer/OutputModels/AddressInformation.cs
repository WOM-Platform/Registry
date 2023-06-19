using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.OutputModels {
    public class AddressInformation {
        public string StreetName { get; set; }

        public string StreetNumber { get; set; }

        public string ZipCode { get; set; }

        public string City { get; set; }

        public string Country { get; set; }

        public string FormattedAddress { get; set; }

        public string GoogleMapsPlaceId { get; set; }
    }

    public static class AddressInformationExtensions {
        public static AddressInformation ToOutput(this AddressBlock addressBlock) {
            return addressBlock == null ? null : new AddressInformation {
                StreetName = addressBlock.StreetName,
                StreetNumber = addressBlock.StreetNumber,
                ZipCode = addressBlock.ZipCode,
                City = addressBlock.City,
                Country = addressBlock.Country,
                FormattedAddress = addressBlock.FormattedAddress,
                GoogleMapsPlaceId = addressBlock.GoogleMapsPlaceId,
            };
        }
    }
}
