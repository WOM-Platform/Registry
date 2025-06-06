using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class BadgeSimpleFilter {
        [BsonElement("count")]
        public int Count { get; set; }

        [BsonElement("sourceId")]
        [BsonIgnoreIfNull]
        public ObjectId? SourceId { get; set; }

        [BsonElement("aim")]
        [BsonIgnoreIfNull]
        public string? Aim { get; set; }

        [BsonElement("bounds")]
        [BsonIgnoreIfNull]
        public Bounds? Bounds { get; set; }

        [BsonElement("interval")]
        [BsonIgnoreIfNull]
        public IntervalSpecification? Interval { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }
}
