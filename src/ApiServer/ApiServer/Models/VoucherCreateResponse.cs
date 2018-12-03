using System;

namespace WomPlatform.Web.Api.Models {

    public class VoucherCreateResponse {

        /// <summary>
        /// Payload signed and encrypted by registry (encoded as <see cref="Content" />).
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// Inner payload signed and encrypted by registry.
        /// </summary>
        public class Content {

            /// <summary>
            /// Identifies the source as an URL.
            /// </summary>
            public string Source { get; set; }

            /// <summary>
            /// Provides the next nonce to use in a voucher creation request.
            /// </summary>
            public string NextNonce { get; set; }

            /// <summary>
            /// Voucher redemption OTC.
            /// </summary>
            public Guid Otc { get; set; }

        }

    }

}
