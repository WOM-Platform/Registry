using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.IdGenerators;
using System;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class Migration {
        [BsonId(IdGenerator = typeof(GuidGenerator))]
        public Guid Id { get; set; }

        [BsonElement("password")]
        public string Password { get; set; }

        [BsonElement("accessCount")]
        [BsonDefaultValue(0)]
        [BsonIgnoreIfDefault]
        public int AccessCount { get; set; } = 0;

        [BsonElement("expiresOn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ExpiresOn { get; set; }

        [BsonElement("completed")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool IsCompleted { get; set; } = false;

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }
}
