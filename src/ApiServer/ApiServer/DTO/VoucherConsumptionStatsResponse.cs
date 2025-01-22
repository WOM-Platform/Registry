using System.Collections.Generic;

namespace WomPlatform.Web.Api.DTO;

public class VoucherConsumptionStatsResponse {
    public int TotalConsumed { get; set; }
    public int TotalEverConsumed { get; set; }
    public List<VoucherByAimDTO> VoucherByAims { get; set; }
    public List<MerchantRankDTO> MerchantRanks { get; set; }
    public List<TotalConsumedOverTimeDto> TotalConsumedOverTime { get; set; }
}
