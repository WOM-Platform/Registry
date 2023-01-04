﻿using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver.GeoJsonObjectModel;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    public class Offer {

        [BsonId(IdGenerator = typeof(GuidGenerator))]
        public Guid Otc { get; set; }

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("cost")]
        public int Cost { get; set; }

        [BsonElement("filter")]
        [BsonIgnoreIfNull]
        public Filter Filter { get; set; }

        public class PosInformation {
            [BsonElement("id")]
            public ObjectId Id { get; set; }

            [BsonElement("name")]
            public string Name { get; set; }

            [BsonElement("description")]
            [BsonIgnoreIfNull]
            public string Description { get; set; }

            [BsonElement("position")]
            public GeoJsonPoint<GeoJson2DGeographicCoordinates> Position { get; set; }
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

        [BsonElement("deactivated")]
        [BsonDefaultValue(false)]
        [BsonIgnoreIfDefault]
        public bool Deactivated { get; set; } = false;

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
