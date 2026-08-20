using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class Campaign {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("isPublic")]
        public bool IsPublic { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("simpleFilter")]
        public CampaignSimpleFilter SimpleFilter { get; set; }

        [BsonElement("womCount")]
        public int WomCount { get; set; }

        [BsonElement("imagePath")]
        [BsonIgnoreIfNull]
        public string? ImagePath { get; set; }

        [BsonElement("description")]
        [BsonIgnoreIfNull]
        public string? Description { get; set; }

        [BsonElement("informationUrl")]
        [BsonIgnoreIfNull]
        public string? InformationUrl { get; set; }

        [BsonElement("startDate")]
        public DateTime StartDate { get; set; }

        [BsonElement("endDate")]
        [BsonIgnoreIfNull]
        public DateTime? EndDate { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }

        [BsonElement("lastUpdate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        [BsonIgnoreIfNull]
        public DateTime? LastUpdate { get; set; }

        [BsonElement("isDeleted")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool IsDeleted { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }
}
