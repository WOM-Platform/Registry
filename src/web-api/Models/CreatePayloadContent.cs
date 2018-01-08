using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models {

    public class CreatePayloadContent {

        public Guid Id { get; set; }

        public int SourceId { get; set; }

        public VoucherRequestInfo[] Vouchers { get; set; }

    }

}
