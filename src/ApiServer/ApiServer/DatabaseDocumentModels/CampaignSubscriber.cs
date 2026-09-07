using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using WomPlatform.Web.Api.OutputModels.Campaign;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class CampaignSubscriber {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("campaignId")]
        public ObjectId CampaignId { get; set; }

        [BsonElement("token")]
        public string Token { get; set; }

        [BsonElement("registeredAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime RegisteredAt { get; set; }

        [BsonElement("lastUsedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastUsedAt { get; set; }

        [BsonElement("isRevoked")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool IsRevoked { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }

    public static class CampaignSubscriberOutputExtensions {
        public static CampaignSubscriberOutput ToOutput(
            this DatabaseDocumentModels.CampaignSubscriber subscriber
        ) {
            return new CampaignSubscriberOutput {
                Token = subscriber.Token,
            };
        }
    }
}
