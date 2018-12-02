using System;

namespace WomPlatform.Web.Api.Models {

    public class VoucherRedeemResponse {

        public Guid Signature { get; set; }

        /// <summary>
        /// Signed payload, encoded as an <see cref="VoucherRedeemResponseContent" /> instance.
        /// </summary>
        public string Payload { get; set; }

    }

}
