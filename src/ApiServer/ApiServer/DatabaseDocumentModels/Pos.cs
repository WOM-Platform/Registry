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

        [BsonElement("publicKey")]
        public string PublicKey { get; set; }

        [BsonElement("privateKey")]
        [BsonIgnoreIfNull]
        public string PrivateKey { get; set; }

        [BsonElement("position")]
        public GeoJsonPoint<GeoJson2DGeographicCoordinates> Position { get; set; }

        [BsonElement("url")]
        [BsonIgnoreIfNull]
        public string Url { get; set; }

        [BsonElement("isDummy")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool IsDummy { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
