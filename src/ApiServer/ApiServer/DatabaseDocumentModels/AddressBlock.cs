using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class AddressBlock {

        [BsonElement("streetName")]
        public string StreetName { get; set; }

        [BsonElement("streetNumber")]
        [BsonIgnoreIfNull]
        public string StreetNumber { get; set; }

        [BsonElement("zipCode")]
        public string ZipCode { get; set; }

        [BsonElement("city")]
        public string City { get; set; }

        [BsonElement("country")]
        public string Country { get; set; }

        [BsonElement("formattedAddress")]
        [BsonIgnoreIfNull]
        public string FormattedAddress { get; set; }

        [BsonElement("googleMapsPlaceId")]
        [BsonIgnoreIfNull]
        public string GoogleMapsPlaceId { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }
}
