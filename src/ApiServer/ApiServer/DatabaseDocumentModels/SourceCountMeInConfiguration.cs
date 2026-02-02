using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WomPlatform.Web.Api.DatabaseDocumentModels {
    public class SourceCountMeInConfiguration {
        [BsonElement("providerId")]
        public string ProviderId { get; set; }

        public class FixedTotemInstallation {
            [BsonElement("totemId", Order = 1)]
            public string TotemId { get; set; }

            [BsonElement("name", Order = 2)]
            public string Name { get; set; }
        }

        [BsonElement("fixedTotems")]
        public FixedTotemInstallation[] FixedTotemInstallations { get; set; } = [];

        [BsonExtraElements]
        public BsonDocument CatchAll { get; set; }
    }
}
