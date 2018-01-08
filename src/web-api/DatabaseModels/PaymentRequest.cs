using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WomPlatform.Web.Api.Models;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("PaymentRequests")]
    public class PaymentRequest {

        [Key]
        public long Id { get; set; }

        public int Amount { get; set; }

        public string JsonFilter { get; set; }

        // TODO: parsed JSON filter access with [Computed]

        public string OTCPay { get; set; }

        public string URLAckPocket { get; set; }

        public string URLAckPOS { get; set; }

        public DateTime CreatedAt { get; set; }

        public string State { get; set; }

        public long PosId { get; set; }

    }

}
