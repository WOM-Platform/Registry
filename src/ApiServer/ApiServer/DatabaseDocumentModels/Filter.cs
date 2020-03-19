using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    /// <summary>
    /// Filter model that encloses supported filter types (only simple ATM).
    /// </summary>
    public class Filter {

        [BsonElement("aims")]
        [BsonIgnoreIfNull]
        public string Aims { get; set; } = null;

        [BsonElement("bounds")]
        [BsonIgnoreIfNull]
        public GeoJsonBoundingBox<GeoJson2DGeographicCoordinates> Bounds { get; set; } = null;

        [BsonElement("maxAge")]
        [BsonIgnoreIfNull]
        public long? MaxAge { get; set; }

        // TODO: add complex filter models

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
