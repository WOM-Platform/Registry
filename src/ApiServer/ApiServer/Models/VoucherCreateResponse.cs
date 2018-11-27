using System;

namespace WomPlatform.Web.Api.Models {

    public class VoucherCreateResponse {

        public string EncryptedOtc { get; set; }

        public int VoucherAmount { get; set; }

    }

}
