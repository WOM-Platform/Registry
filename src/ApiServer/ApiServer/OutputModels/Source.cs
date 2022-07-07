using System.Collections.Generic;
using System.Collections.Immutable;
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
        public string[] EnabledAims { get; init; }

        public ImmutableDictionary<string, int> PerAimBudget { get; init; }

        public Location DefaultLocation { get; init; }

        public bool LocationIsFixed { get; init; }
    }

    public record SourceBudgetOutput {
        public List<string> EnabledAims { get; init; }

        public Dictionary<string, int> PerAimBudget { get; init; }
    }

    public record SourceGeneratedCountOutput {
        public int Total { get; set; }
    }

    public static class SourceOutputHelpers {

        public static SourceOutput ToOutput(this DatabaseDocumentModels.Source source) {
            return new SourceOutput {
                Id = source.Id.ToString(),
                Name = source.Name,
                Url = source.Url
            };
        }

        public static SourceLoginV2Output ToLoginV2Output(this DatabaseDocumentModels.Source source, string[] allAims = null) {
            return new SourceLoginV2Output {
                Id = source.Id.ToString(),
                Name = source.Name,
                Url = source.Url,
                PrivateKey = source.PrivateKey,
                PublicKey = source.PublicKey,
                EnabledAims = (source.Aims.EnableAll ? allAims : source.Aims.Enabled).ToSafeArray(),
                PerAimBudget = source.Aims.CurrentBudget.ToSafeImmutableDictionary(),
                DefaultLocation = (source.Location.Position == null) ? null : new Location {
                    Latitude = source.Location.Position.Coordinates.Latitude,
                    Longitude = source.Location.Position.Coordinates.Longitude
                },
                LocationIsFixed = source.Location.IsFixed
            };
        }

    }

}
