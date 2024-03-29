﻿namespace WomPlatform.Web.Api.OutputModels.Stats {

    public class VoucherSourceStatsResponse {

        public long GenerationRequests { get; init; }

        public long TotalVouchersGenerated { get; init; }

        public long TotalVouchersRedeemed { get; init; }

    }

}
