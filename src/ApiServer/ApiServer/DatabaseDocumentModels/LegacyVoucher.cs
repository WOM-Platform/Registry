using System;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver.GeoJsonObjectModel;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    public class LegacyVoucher {

        [BsonId(IdGenerator = typeof(NullIdChecker))]
        public long Id { get; set; }

        [BsonElement("secret")]
        public string Secret { get; set; }

        [BsonElement("aimCode")]
        public string AimCode { get; set; }

        [BsonElement("position")]
        public GeoJsonPoint<GeoJson2DGeographicCoordinates> Position { get; set; }

        [BsonElement("timestamp")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; }

        [BsonElement("spent")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool Spent { get; set; } = false;

    }

}
