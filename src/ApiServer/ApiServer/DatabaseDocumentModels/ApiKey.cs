using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class ApiKey {
        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("key")]
        [BsonRequired]
        public string Key { get; set; }

        [BsonElement("controlledEntityId")]
        [BsonRequired]
        public ObjectId ControlledEntityId { get; set; }

        [BsonElement("selector")]
        [BsonIgnoreIfNull]
        public string KeySelector { get; set; }

        public enum KindOfKey {
            SourceAdministrator
        }

        [BsonElement("kind")]
        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        public KindOfKey Kind { get; set; }

        [BsonElement("expired")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool Expired { get; set; }

        [BsonElement("publicKey")]
        public string PublicKey { get; set; }

        [BsonElement("privateKey")]
        public string PrivateKey { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }
}
