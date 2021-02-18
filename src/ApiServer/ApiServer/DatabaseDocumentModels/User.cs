using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    public class User {

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("surname")]
        public string Surname { get; set; }

        [BsonElement("verificationToken")]
        [BsonIgnoreIfNull]
        public string VerificationToken { get; set; }

        [BsonElement("passwordResetToken")]
        [BsonIgnoreIfNull]
        public string PasswordResetToken { get; set; }

        [BsonElement("registeredOn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonIgnoreIfNull]
        public DateTime? RegisteredOn { get; set; }

        [BsonElement("lastUpdate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonIgnoreIfNull]
        public DateTime? LastUpdate { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
