using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class CampaignContribution {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("token")]
        public string Token { get; set; }

        [BsonElement("campaignId")]
        public ObjectId CampaignId { get; set; }

        [BsonElement("contributedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ContributedAt { get; set; }

        [BsonElement("womCount")]
        public int WomCount { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }
}
