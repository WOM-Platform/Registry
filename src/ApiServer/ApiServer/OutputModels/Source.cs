using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WomPlatform.Web.Api.OutputModels {

    public record SourceOutput {
        public string Id { get; init; }

        public string Name { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Url { get; init; }
    }

    public record SourceLoginOutput : SourceOutput {
        public string PrivateKey { get; init; }

        public string PublicKey { get; init; }
    }

    public record SourceLoginV2Output : SourceLoginOutput {
        public List<string> EnabledAims { get; init; }

        public Dictionary<string, int> PerAimBudget { get; init; }

        public Location DefaultLocation { get; init; }

        public bool LocationIsFixed { get; init; }
    }

    public record SourceBudgetOutput {
        public List<string> EnabledAims { get; init; }

        public Dictionary<string, int> PerAimBudget { get; init; }
    }

    public static class SourceOutputHelpers {

        public static SourceOutput ToOutput(this DatabaseDocumentModels.Source source) {
            return new SourceOutput {
                Id = source.Id.ToString(),
                Name = source.Name,
                Url = source.Url
            };
        }

    }

}
