using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace WomPlatform.Web.Api.OutputModels.Source {
    public class SourceAuthDetailsOutput : SourceAuthOutput {
        public SourceAuthDetailsOutput(
            DatabaseDocumentModels.Source source,
            string[] allAims,
            PictureOutput customGeneratorPicture
        ) : base(source) {

            string[] enabledAims = (source.Aims?.EnableAll ?? false || (source.Aims?.Enabled ?? []).Length == 0) ?
                allAims :
                source.Aims?.Enabled;

            EnabledAims = enabledAims.ToSafeArray();
            PerAimBudget = (source.Aims?.CurrentBudget).ToSafeImmutableDictionary();

            Aims = new AimDetails {
                Enabled = enabledAims.ToSafeArray(),
                Budget = (source.Aims?.CurrentBudget).ToSafeImmutableDictionary(),
            };

            //TODO: link to correct generator
            CustomGenerator = source.CustomGenerator.ToOutput(customGeneratorPicture);
        }

        [Obsolete]
        public string[] EnabledAims { get; init; }

        [Obsolete]
        public ImmutableDictionary<string, int> PerAimBudget { get; init; }

        public class AimDetails {
            public string[] Enabled { get; init; }

            public ImmutableDictionary<string, int> Budget { get; init; }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public AimDetails Aims { get; init; }

        public SourceCustomGeneratorOutput CustomGenerator { get; init; }
    }
}
