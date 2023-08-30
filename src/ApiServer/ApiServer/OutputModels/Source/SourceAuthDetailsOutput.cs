using System.Collections.Immutable;

namespace WomPlatform.Web.Api.OutputModels.Source {
    public class SourceAuthDetailsOutput : SourceAuthOutput {
        public SourceAuthDetailsOutput(
            DatabaseDocumentModels.Source source,
            string[] allAims,
            PictureOutput customGeneratorPicture
        ) : base(source) {

            EnabledAims = (source.Aims.EnableAll ? allAims : source.Aims.Enabled).ToSafeArray();
            PerAimBudget = source.Aims.CurrentBudget.ToSafeImmutableDictionary();

            CustomGenerator = source.CustomGenerator.ToOutput(customGeneratorPicture);
        }

        public string[] EnabledAims { get; init; }

        public ImmutableDictionary<string, int> PerAimBudget { get; init; }

        public SourceCustomGeneratorOutput CustomGenerator { get; init; }
    }
}
