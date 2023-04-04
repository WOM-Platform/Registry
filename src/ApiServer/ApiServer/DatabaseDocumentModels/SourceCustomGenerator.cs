using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class SourceCustomGenerator {

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("logoPath")]
        [BsonIgnoreIfNull]
        public string LogoPath { get; set; }

        [BsonElement("logoBlurHash")]
        [BsonIgnoreIfNull]
        public string LogoBlurHash { get; set; }

        [BsonElement("enableCustomGeneration")]
        public bool EnableCustomGeneration { get; set; } = true;

        public class TemplateInfo {

            [BsonElement("name")]
            public string Name { get; set; }

            [BsonElement("description")]
            [BsonIgnoreIfNull]
            public string Description { get; set; }

            [BsonElement("guide")]
            [BsonIgnoreIfNull]
            public string Guide { get; set; }

            [BsonElement("presetWomCount")]
            [BsonIgnoreIfNull]
            public int? PresetWomCount { get; set; }

            [BsonElement("presetWomAim")]
            [BsonIgnoreIfNull]
            public string PresetWomAim { get; set; }

            [BsonElement("presetWomLocation")]
            [BsonIgnoreIfNull]
            public GeoJsonPoint<GeoJson2DGeographicCoordinates> PresetWomLocation { get; set; }

            [BsonExtraElements]
            public BsonDocument CatchAll { get; set; }

        }

        [BsonElement("templates")]
        [BsonIgnoreIfDefault]
        public TemplateInfo[] Templates { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }
}
