using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class CampaignSimpleFilter : BaseSimpleFilter {
        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }
}
