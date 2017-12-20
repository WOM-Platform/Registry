using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WomPlatform.Web.Api.Models;

namespace WomPlatform.Web.Api.DatabaseModels
{
    public class PaymentRequest
    {
        public long Id { get; set; }
        public long POS_Id { get; set; }
        public string URLAckPocket { get; set; }
        public int Amount { get; set; }
        public string URLAckPOS { get; set; }
        public string OTCPay { get; set; }
        public DateTime CreatedAt { get; set; }
        public string State { get; set; }
        public FilterInfo JsonFilter { get; set; }
    }
}
