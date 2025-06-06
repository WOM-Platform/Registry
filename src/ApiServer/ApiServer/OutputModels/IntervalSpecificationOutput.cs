using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WomPlatform.Web.Api.InputModels {
    public class IntervalSpecificationOutput {
        [BsonElement("start")]
        public DateTime Start { get; set; }

        [BsonElement("end")]
        public DateTime End { get; set; }
    }

    public static class IntervalSpecificationExtensions {
        public static IntervalSpecificationOutput ToOutput(this DatabaseDocumentModels.IntervalSpecification? interval) {
            if(interval == null) {
                return null;
            }

            return new IntervalSpecificationOutput {
                Start = interval.Start,
                End = interval.End,
            };
        }
    }
}
