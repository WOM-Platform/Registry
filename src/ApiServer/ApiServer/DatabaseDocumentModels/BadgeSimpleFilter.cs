using System;
using MongoDB.Bson.Serialization.Attributes;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class BadgeSimpleFilter {
        [BsonElement("count")]
        public int Count { get; set; }

        [BsonElement("aim")]
        [BsonIgnoreIfDefault]
        public string? Aim { get; set; }

        [BsonElement("interval")]
        [BsonIgnoreIfNull]
        public IntervalSpecification? Interval { get; set; }

        public class IntervalSpecification {
            [BsonElement("start")]
            public DateTime Start { get; set; }

            [BsonElement("end")]
            public DateTime End { get; set; }
        }
    }
}
