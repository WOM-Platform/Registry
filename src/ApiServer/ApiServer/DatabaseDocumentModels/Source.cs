using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver.GeoJsonObjectModel;

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

        public class SourceAims {
            [BsonElement("enableAll")]
            [BsonIgnoreIfDefault]
            [BsonDefaultValue(false)]
            public bool EnableAll { get; set; } = false;

            [BsonElement("enabled")]
            public string[] Enabled { get; set; }

            [BsonElement("budget")]
            public Dictionary<string, int> CurrentBudget { get; set; }

            [BsonExtraElements]
            public BsonDocument CatchAll { get; set; }
        }

        [BsonElement("aims")]
        [BsonIgnoreIfNull]
        public SourceAims Aims { get; set; } = new SourceAims();

        public class SourceLocation {
            [BsonElement("default")]
            [BsonIgnoreIfNull]
            public GeoJsonPoint<GeoJson2DGeographicCoordinates> Position { get; set; }

            [BsonElement("isFixed")]
            [BsonIgnoreIfDefault]
            [BsonDefaultValue(false)]
            public bool IsFixed { get; set; } = false;
        }

        [BsonElement("location")]
        [BsonIgnoreIfNull]
        public SourceLocation Location { get; set; } = new SourceLocation();

        [BsonElement("adminUserIds")]
        [BsonIgnoreIfNull]
        public ObjectId[] AdministratorUserIds { get; set; }

        [BsonElement("customGenerator")]
        public SourceCustomGenerator CustomGenerator { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
