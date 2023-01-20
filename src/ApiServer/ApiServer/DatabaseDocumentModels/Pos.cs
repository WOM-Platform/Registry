using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver.GeoJsonObjectModel;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    public class Pos {

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("merchantId")]
        public ObjectId MerchantId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("description")]
        [BsonIgnoreIfNull]
        public string Description { get; set; }

        [BsonElement("publicKey")]
        public string PublicKey { get; set; }

        [BsonElement("privateKey")]
        [BsonIgnoreIfNull]
        public string PrivateKey { get; set; }

        [BsonElement("position")]
        public GeoJsonPoint<GeoJson2DGeographicCoordinates> Position { get; set; }

        [BsonElement("coverPath")]
        [BsonIgnoreIfNull]
        public string CoverPath { get; set; }

        [BsonElement("coverBlurHash")]
        [BsonIgnoreIfNull]
        public string CoverBlurHash { get; set; }

        [BsonElement("url")]
        [BsonIgnoreIfNull]
        public string Url { get; set; }

        [BsonElement("createdOn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonIgnoreIfNull]
        public DateTime? CreatedOn { get; set; }

        [BsonElement("lastUpdate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonIgnoreIfNull]
        public DateTime? LastUpdate { get; set; }

        [BsonElement("isDummy")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool IsDummy { get; set; } = false;

        [BsonElement("isActive")]
        [BsonDefaultValue(true)]
        [BsonIgnoreIfDefault]
        public bool IsActive { get; set; } = true;

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
