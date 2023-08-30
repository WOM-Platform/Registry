namespace WomPlatform.Web.Api.OutputModels.Source {
    public class SourceAuthOutput : SourceOutput {
        public SourceAuthOutput(DatabaseDocumentModels.Source source)
            : base(source) {

            PublicKey = source.PublicKey;
            PrivateKey = source.PrivateKey;
        }

        public string PublicKey { get; init; }

        public string PrivateKey { get; init; }
    }
}
