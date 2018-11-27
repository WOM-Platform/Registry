using System;

namespace WomPlatform.Web.Api.Models {

    /// <summary>
    /// Body payload for voucher creation.
    /// </summary>
    public class VoucherCreatePayload {

        /// <summary>
        /// Unique ID of the source.
        /// </summary>
        public uint SourceId { get; set; }

        /// <summary>
        /// Nonce to prevent repetition.
        /// </summary>
        public Guid Nonce { get; set; }

        /// <summary>
        /// Encrypted payload (resolves to <see cref="VoucherCreatePayloadContent" />
        /// </summary>
        public string Payload { get; set; }

    }

}
