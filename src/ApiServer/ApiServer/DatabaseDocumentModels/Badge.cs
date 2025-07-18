﻿using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class Badge {
        [BsonId(IdGenerator = typeof(ObjectIdGenerator))]
        public ObjectId Id { get; set; }

        [BsonElement("sortName")]
        public string SortName { get; set; }

        [BsonElement("challengeId")]
        public ObjectId? ChallengeId { get; set; }

        [BsonElement("isPublic")]
        public bool IsPublic { get; set; }

        [BsonElement("name")]
        public Dictionary<string, string> Name { get; set; }

        [BsonElement("simpleFilter")]
        public BadgeSimpleFilter SimpleFilter { get; set; }

        [BsonElement("imagePath")]
        [BsonIgnoreIfNull]
        public string? ImagePath { get; set; }

        [BsonElement("imageBlurHash")]
        [BsonIgnoreIfNull]
        public string? ImageBlurHash { get; set; }

        [BsonElement("description")]
        public Dictionary<string, string> Description { get; set; }

        [BsonElement("informationUrl")]
        [BsonIgnoreIfNull]
        public string? InformationUrl { get; set; }

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
