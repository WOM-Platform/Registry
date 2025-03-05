using System.Collections.Generic;

namespace WomPlatform.Web.Api.DTO {
    public class VoucherGenerationRedemptionStatsResponse {
        public int GeneratedInPeriod { get; set; }
        public int TotalGenerated { get; set; }
        public int RedeemedInPeriod { get; set; }
        public int TotalRedeemed { get; set; }
        public List<VoucherByAimDTO> VoucherByAim { get; set; }
        public List<TotalGeneratedAndRedeemedOverTimeDto> TotalGeneratedAndRedeemedOverTime { get; set; }
        public List<SourceRankDTO> SourceRank { get; set; }
    }
}
