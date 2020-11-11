using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver.GeoJsonObjectModel;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    public class Voucher {

        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("secret")]
        public string Secret { get; set; }

        [BsonElement("aimCode")]
        public string AimCode { get; set; }

        [BsonElement("position")]
        public GeoJsonPoint<GeoJson2DGeographicCoordinates> Position { get; set; }

        [BsonElement("timestamp")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; }

        [BsonElement("count")]
        [BsonDefaultValue(1)]
        [BsonIgnoreIfDefault]
        public int Count { get; set; } = 1;

        [BsonElement("initialCount")]
        public int InitialCount { get; set; }

        [BsonElement("generationRequestId")]
        public Guid GenerationRequestId { get; set; }

    }

}
