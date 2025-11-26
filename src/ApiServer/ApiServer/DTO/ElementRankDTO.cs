using System.Collections.Generic;
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

        public List<AimBreakdownDTO> AimBreakdown { get; set; }
    }

    public class AimBreakdownDTO {
        public string AimCode { get; set; }
        public int Sum { get; set; }
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
