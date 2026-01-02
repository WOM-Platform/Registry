using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class CheckInTotem {
        [BsonId(IdGenerator = typeof(NullIdChecker))]
        public string EventId { get; set; }

        [BsonRequired]
        [BsonElement("providerId")]
        public string ProviderId { get; set; }

        [BsonRequired]
        [BsonElement("totemId")]
        public string TotemId { get; set; }

        [BsonRequired]
        public ObjectId SourceId { get; set; }

        [BsonRequired]
        [BsonElement("createdOn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedOn { get; set; }

        [BsonElement("lastUpdate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonIgnoreIfNull]
        public DateTime? LastUpdate { get; set; }

        [BsonElement("deleted")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool Deleted { get; set; } = false;

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }
}
