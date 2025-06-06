using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class IntervalSpecification {
        [BsonElement("start")]
        public DateTime Start { get; set; }

        [BsonElement("end")]
        public DateTime End { get; set; }

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }
}
