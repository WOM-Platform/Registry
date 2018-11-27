using System;

namespace WomPlatform.Web.Api.Models {

    /// <summary>
    /// Inner payload of <see cref="VoucherCreatePayload" />.
    /// </summary>
    public class VoucherCreatePayloadContent {

        /// <summary>
        /// Unique ID of the source.
        /// </summary>
        public uint SourceId { get; set; }

        /// <summary>
        /// Nonce to prevent repetition.
        /// </summary>
        public Guid Nonce { get; set; }

        /// <summary>
        /// Encapsulates information about a single voucher.
        /// </summary>
        public class VoucherInfo {

            public double Latitude { get; set; }

            public double Longitude { get; set; }

            public DateTime Timestamp { get; set; }

        }

        /// <summary>
        /// Details of the vouchers to create.
        /// </summary>
        public VoucherInfo[] Vouchers { get; set; }

    }

}
