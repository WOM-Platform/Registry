using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models {

    public class CreatePayload {

        public int SourceId { get; set; }

        public Guid Nonce { get; set; }

        public string Payload { get; set; }

    }
}
