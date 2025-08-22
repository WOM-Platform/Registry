using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.OutputModels.Offers {
    public class OfferAdminOutput {
          [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("description")]
        public string? Description { get; set; }

        public class PaymentInformation {
            [BsonElement("otc")]
            public Guid Otc { get; set; }

            [BsonElement("password")]
            public string Password { get; set; }

            [BsonElement("cost")]
            public int Cost { get; set; }

            [BsonElement("filter")]
            [BsonIgnoreIfNull]
            public Filter? Filter { get; set; }
        }

        [BsonElement("payment")]
        public PaymentInformation Payment { get; set; }

        [BsonElement("paymentRequestId")]
        [BsonIgnoreIfDefault]
        [Obsolete]
        public Guid? PaymentRequestId { get; set; }

        [BsonElement("cost")]
        [BsonIgnoreIfDefault]
        [Obsolete]
        public int Cost { get; set; }

        [BsonElement("filter")]
        [BsonIgnoreIfNull]
        [Obsolete]
        public Filter? Filter { get; set; }

        public class PosInformation {
            [BsonElement("id")]
            public ObjectId Id { get; set; }

            [BsonElement("name")]
            public string Name { get; set; }

            [BsonElement("description")]
            [BsonIgnoreIfNull]
            public string? Description { get; set; }

            [BsonElement("coverPath")]
            [BsonIgnoreIfNull]
            public string? CoverPath { get; set; }

            [BsonElement("coverBlurHash")]
            [BsonIgnoreIfNull]
            public string? CoverBlurHash { get; set; }

            [BsonElement("position")]
            public GeoJsonPoint<GeoJson2DGeographicCoordinates> Position { get; set; }

            [BsonElement("url")]
            [BsonIgnoreIfNull]
            public string? Url { get; set; }
        }

        [BsonElement("pos")]
        public PosInformation Pos { get; set; }

        public class MerchantInformation {
            [BsonElement("id")]
            public ObjectId Id { get; set; }

            [BsonElement("name")]
            public string Name { get; set; }

            [BsonElement("website")]
            public string WebsiteUrl { get; set; }
        }

        [BsonElement("merchant")]
        public MerchantInformation Merchant { get; set; }

        [BsonElement("createdOn")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedOn { get; set; }

        [BsonElement("lastUpdate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastUpdate { get; set; }

        [BsonElement("lastPaymentConfirmation")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastPaymentConfirmation { get; set; }


    }
}
