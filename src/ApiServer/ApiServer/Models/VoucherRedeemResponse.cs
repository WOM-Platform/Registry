using System;

namespace WomPlatform.Web.Api.Models {

    public class VoucherRedeemResponse {

        public Guid Nonce { get; set; }

        /// <summary>
        /// Encapsulates info about a single voucher.
        /// </summary>
        public class VoucherInfo {

            public uint Id { get; set; }

            public string Secret { get; set; }

            public double Latitude { get; set; }

            public double Longitude { get; set; }

            public DateTime Timestamp { get; set; }

            public string Source { get; set; }

        }

        public VoucherInfo[] Vouchers { get; set; }

    }

}
