using System.Collections.Generic;

namespace WomPlatform.Web.Api.OutputModels.Stats {

    public class VouchersGeneralStatsResponse {

        public long TotalVouchersGenerated { get; init; }

        public long TotalVouchersRedeemed { get; init; }

        public long TotalVouchersAvailable { get; init; }

        public long TotalVouchersSpent { get; init; }

        public class VouchersByAimStatsResponse {
            public long Generated { get; init; }

            public long Redeemed { get; init; }

            public long Available { get; init; }

            public long Spent { get; init; }
        }

        public IDictionary<string, VouchersByAimStatsResponse> Aims { get; set; }

    }

}
