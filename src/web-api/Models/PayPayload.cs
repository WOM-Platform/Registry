using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models
{
    public class PayPayload
    {
        public string OTCPay { get; set; }

        public Guid[] vouchers { get; set; }

    }
}
