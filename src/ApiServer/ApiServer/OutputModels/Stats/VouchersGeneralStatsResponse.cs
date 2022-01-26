using System.Collections.Generic;

namespace WomPlatform.Web.Api.OutputModels.Stats {

    public class VouchersGeneralStatsResponse {

        public long TotalVouchersGenerated { get; set; }

        public long TotalVouchersRedeemed { get; set; }

        public long TotalVouchersAvailable { get; set; }

        public class VouchersByAimStatsResponse {
            public long Generated { get; set; }

            public long Redeemed { get; set; }

            public long Available { get; set; }
        }

        public IDictionary<string, VouchersByAimStatsResponse> Aims { get; set; }

    }

}
