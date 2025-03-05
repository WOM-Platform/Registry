using System.Collections.Generic;

namespace WomPlatform.Web.Api.DTO {
    public class VoucherConsumptionStatsResponse {
        public int ConsumedInPeriod { get; set; }
        public int TotalConsumed { get; set; }
        public int TransactionsInPeriod { get; set; }
        public int TotalTransactions { get; set; }
        public List<VoucherByAimDTO> VoucherByAims { get; set; }
        public List<MerchantRankDTO> MerchantRanks { get; set; }
        public List<MerchantRankOvertimeDTO> MerchantOvertimeRanks { get; set; }
        public List<TotalConsumedOverTimeDto> TotalConsumedOverTime { get; set; }
    }
}
