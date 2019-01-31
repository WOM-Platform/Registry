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
            /// Identifies the registry.
            /// </summary>
            public string RegistryUrl { get; set; }

            /// <summary>
            /// Returns the nonce originally sent by the source.
            /// </summary>
            public string Nonce { get; set; }

            /// <summary>
            /// Voucher redemption OTC.
            /// </summary>
            public Guid Otc { get; set; }

        }

    }

}
