using System;

namespace WomPlatform.Web.Api.Models {

    /// <summary>
    /// Request payload for voucher creation.
    /// </summary>
    public class VoucherCreatePayload {

        /// <summary>
        /// Unique ID of the source.
        /// </summary>
        public long SourceId { get; set; }

        /// <summary>
        /// Nonce to prevent repetition (base64-encoded).
        /// </summary>
        public string Nonce { get; set; }

        /// <summary>
        /// Payload signed and encrypted by source (encoded as <see cref="Content" />).
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// Inner payload signed and encrypted by source.
        /// </summary>
        public class Content {

            /// <summary>
            /// Unique ID of the source.
            /// </summary>
            public long SourceId { get; set; }

            /// <summary>
            /// Nonce to prevent repetition (base64-encoded).
            /// </summary>
            public string Nonce { get; set; }

            /// <summary>
            /// Password specified by user.
            /// </summary>
            public string Password { get; set; }

            /// <summary>
            /// Details of the vouchers to create.
            /// </summary>
            public VoucherInfo[] Vouchers { get; set; }

        }

        /// <summary>
        /// Encapsulates information about a single voucher.
        /// </summary>
        public class VoucherInfo {

            public string Aim { get; set; }

            public double Latitude { get; set; }

            public double Longitude { get; set; }

            public DateTime Timestamp { get; set; }

        }

    }

}
