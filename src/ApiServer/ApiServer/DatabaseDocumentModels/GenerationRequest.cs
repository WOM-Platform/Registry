﻿using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class GenerationRequest {
        [BsonId(IdGenerator = typeof(GuidGenerator))]
        public Guid Otc { get; set; }

        /// <summary>
        /// Number of voucher specifications used for this generation.
        /// </summary>
        [BsonElement("amount")]
        public int Amount { get; set; }

        /// <summary>
        /// Total of generated vouchers.
        /// </summary>
        [BsonElement("totalVoucherCount")]
        [BsonIgnoreIfNull]
        public int? TotalVoucherCount { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }

        [BsonElement("isVerified")]
        [BsonDefaultValue(false)]
        public bool Verified { get; set; } = false;

        [BsonElement("performedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonIgnoreIfNull]
        public DateTime? PerformedAt { get; set; } = null;

        [BsonElement("isVoid")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool Void { get; set; } = false;

        [BsonElement("attempts")]
        [BsonDefaultValue(1)] // Older requests without numbered attempts default to one attempt
        [BsonIgnoreIfDefault]
        public int Attempts { get; set; }

        [BsonElement("sourceId")]
        public ObjectId SourceId { get; set; }

        [BsonElement("nonce")]
        public string Nonce { get; set; }

        [BsonElement("password")]
        public string Password { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }
}
