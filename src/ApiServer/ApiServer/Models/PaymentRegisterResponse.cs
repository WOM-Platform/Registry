using System;

namespace WomPlatform.Web.Api.Models {

    public class PaymentRegisterResponse {

        /// <summary>
        /// Encrypted paylod (instance of <see cref="Content" />).
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// Inner payload.
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
