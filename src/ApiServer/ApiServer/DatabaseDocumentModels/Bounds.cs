using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class Bounds {
        [BsonElement("leftTop")]
        public GeoJson2DGeographicCoordinates LeftTop { get; set; }

        [BsonElement("rightBottom")]
        public GeoJson2DGeographicCoordinates RightBottom { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }
}
