using MongoDB.Bson;

namespace WomPlatform.Web.Api.DTO {
    public abstract class RankDTO {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public int Rank { get; set; }
    }

    public class SourceRankDTO : RankDTO {
        public int TotalGeneratedAmount { get; set; }
        public int TotalRedeemedAmount { get; set; }
    }

    public class MerchantRankDTO : RankDTO {
        public int Amount { get; set; }
        public int NumberTransactions { get; set; }
    }

    public class MerchantRankOvertimeDTO {
        public string Id { get; set; }
        public int Amount { get; set; }
        public string Date { get; set; }
        public string MerchantId { get; set; }
        public string MerchantName { get; set; }
    }
}
