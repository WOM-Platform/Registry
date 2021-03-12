using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    public class Session {

        [BsonId(IdGenerator = typeof(GuidGenerator))]
        public Guid Id { get; set; }

        [BsonElement("userId")]
        public ObjectId UserId { get; set; }

        [BsonElement("startedOn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime StartedOn { get; set; }

        [BsonElement("client")]
        [BsonIgnoreIfNull]
        public string Client { get; set; }

        [BsonElement("clientUserAgent")]
        [BsonIgnoreIfNull]
        public string UserAgent { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
