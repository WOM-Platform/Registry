using WomPlatform.Web.Api.OutputModels.Source;

namespace WomPlatform.Web.Api.OutputModels.Authentication {
    public class SourceDashboardOutput {
        public string Name { get; init; }

        public string Surname { get; init; }

        public string Email { get; init; }

        public SourceAuthDetailsOutput[] Sources { get; init; }
    }
}
