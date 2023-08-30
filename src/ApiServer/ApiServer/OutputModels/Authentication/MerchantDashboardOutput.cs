using WomPlatform.Web.Api.OutputModels.Merchant;

namespace WomPlatform.Web.Api.OutputModels.Authentication {
    public class MerchantDashboardOutput {
        public string Name { get; init; }

        public string Surname { get; init; }

        public string Email { get; init; }

        public MerchantAuthOutput[] Merchants { get; init; }
    }
}
