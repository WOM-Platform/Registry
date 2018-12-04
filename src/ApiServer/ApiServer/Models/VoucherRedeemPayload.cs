using System;

namespace WomPlatform.Web.Api.Models {

    public class VoucherRedeemPayload {

        /// <summary>
        /// Payload signed and encrypted (encoded as <see cref="Content" />).
        /// </summary>
        public string Payload { get; set; }

        public class Content {

            public Guid Otc { get; set; }

        }

    }

}
