using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    public class PaymentConfirmation {

        [BsonElement("performedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime PerformedAt { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
