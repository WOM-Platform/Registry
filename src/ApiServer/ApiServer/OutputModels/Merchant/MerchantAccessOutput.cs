using MongoDB.Bson;

namespace WomPlatform.Web.Api.OutputModels.Merchant {
    public class MerchantAccessOutput {
        public class UserAccessInformation {
            public ObjectId UserId { get; set; }

            public string Email { get; set; }

            public string Name { get; set; }

            public string Surname { get; set; }

            public MerchantRole Role { get; set; }
        }

        public ObjectId MerchantId { get; set; }

        public UserAccessInformation[] Users { get; set; }
    }
}
