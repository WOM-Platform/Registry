using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {

    /// <summary>
    /// Filter model that encloses supported filter types (only simple ATM).
    /// </summary>
    public class Filter {

        [BsonElement("aims")]
        [BsonIgnoreIfNull]
        public string Aims { get; set; }

        [BsonElement("bounds")]
        [BsonIgnoreIfNull]
        public Bounds Bounds { get; set; }

        [BsonElement("maxAge")]
        [BsonIgnoreIfNull]
        public long? MaxAge { get; set; }

        // TODO: add complex filter models

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }

    }

}
