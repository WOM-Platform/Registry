using System;
using System.Collections.Generic;

namespace WomPlatform.Web.Api.DTO;

public class VoucherGenerationRedemptionStatsResponse {
    public int TotalGenerated { get; set; }
    public int TotalRedeemed { get; set; }
    public List<VoucherByAimDTO> VoucherByAim { get; set; }
    public int VoucherAvailable { get; set; }
    public List<TotalGeneratedAndRedeemedOverTimeDto> TotalGeneratedAndRedeemedOverTime { get; set; }
}

public class VoucherDetails {
    public string Aim { get; set; }
    public int Count { get; set; }
    public DateTime Date { get; set; }
}
