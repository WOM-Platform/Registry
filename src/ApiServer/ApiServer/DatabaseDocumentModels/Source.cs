using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    public class Source {

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("publicKey")]
        public string PublicKey { get; set; }

        [BsonElement("privateKey")]
        [BsonIgnoreIfNull]
        public string PrivateKey { get; set; }

        [BsonElement("url")]
        [BsonIgnoreIfNull]
        public string Url { get; set; }

        [BsonElement("createdOn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonIgnoreIfNull]
        public DateTime? CreatedOn { get; set; }

        [BsonElement("adminUserIds")]
        public ObjectId[] AdministratorUserIds { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
