using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models {

    public class CreateResponse {

        // What is this?
        public Guid Id { get; set; }

        public string OtcGen { get; set; }

        public DateTime Timestamp { get; set; }

    }

}
