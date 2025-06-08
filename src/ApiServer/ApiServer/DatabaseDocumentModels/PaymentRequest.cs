using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class PaymentRequest {
        [BsonId(IdGenerator = typeof(GuidGenerator))]
        public Guid Otc { get; set; }

        [BsonElement("amount")]
        public int Amount { get; set; }

        [BsonElement("filter")]
        [BsonIgnoreIfNull]
        public Filter Filter { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }

        [BsonElement("isVerified")]
        [BsonDefaultValue(false)]
        public bool Verified { get; set; } = false;

        [BsonElement("isPersistent")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool IsPersistent { get; set; } = false;

        [BsonElement("merchantId")]
        public ObjectId MerchantId { get; set; }

        [BsonElement("posId")]
        public ObjectId PosId { get; set; }

        [BsonElement("nonce")]
        [BsonIgnoreIfNull]
        public string Nonce { get; set; }

        [BsonElement("password")]
        public string Password { get; set; }

        [BsonElement("ackUrlPocket")]
        [BsonIgnoreIfNull]
        public string AckUrlPocket { get; set; }

        [BsonElement("ackUrlPos")]
        [BsonIgnoreIfNull]
        public string AckUrlPos { get; set; }

        [BsonElement("confirmations")]
        [BsonIgnoreIfNull]
        public List<PaymentConfirmation> Confirmations { get; set; } = null;

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }
}
