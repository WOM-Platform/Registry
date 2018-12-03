using System;

namespace WomPlatform.Web.Api.Models {

    public class VoucherCreateResponse {

        public string Payload { get; set; }

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
