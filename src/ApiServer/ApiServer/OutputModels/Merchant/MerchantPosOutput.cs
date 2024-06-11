using MongoDB.Bson;
using WomPlatform.Web.Api.OutputModels.Pos;

namespace WomPlatform.Web.Api.OutputModels.Merchant {
    public class MerchantPosOutput {
        public ObjectId MerchantId { get; set; }

        public string MerchantName { get; set; }

        public PosAuthOutput[] Pos { get; set; }
    }
}
