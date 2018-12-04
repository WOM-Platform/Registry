using System;

namespace WomPlatform.Web.Api.Models {

    public class VoucherRedeemResponse {

        /// <summary>
        /// Signed payload, encoded as an <see cref="Content" /> instance.
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// Payload signed by the registry.
        /// </summary>
        public class Content {

            public VoucherInfo[] Vouchers { get; set; }

        }

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

    }

}
