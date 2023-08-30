using MongoDB.Bson;
using WomPlatform.Web.Api.OutputModels.Merchant;
using WomPlatform.Web.Api.OutputModels.Source;

namespace WomPlatform.Web.Api.OutputModels.User {
    public class UserDetailedOutput {
        public ObjectId Id { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

        public string Surname { get; set; }

        public bool Verified { get; set; }

        public MerchantAuthOutput[] Merchants { get; set; }

        public SourceAuthDetailsOutput[] Sources { get; set; }
    }
}
