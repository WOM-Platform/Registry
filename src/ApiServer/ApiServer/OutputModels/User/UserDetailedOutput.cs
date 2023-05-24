using MongoDB.Bson;

namespace WomPlatform.Web.Api.OutputModels.User {
    public class UserDetailedOutput {
        public ObjectId Id { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

        public string Surname { get; set; }

        public bool Verified { get; set; }

        public MerchantAuthOutput[] Merchants { get; set; }

        public SourceLoginV2Output[] Sources { get; set; }
    }
}
