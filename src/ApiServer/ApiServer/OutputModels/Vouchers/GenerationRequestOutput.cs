using System;

namespace WomPlatform.Web.Api.OutputModels.Vouchers {
    public class GenerationRequestOutput {
        public string RegistryUrl { get; set; }

        public string Nonce { get; set; }

        public Guid Otc { get; set; }

        public string Password { get; set; }

        public string Link { get; set; }

        public int Count { get; set; }
    }
}
