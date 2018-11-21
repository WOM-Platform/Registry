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
        public uint Id { get; set; }

        public ushort Amount { get; set; }

        public string JsonFilter { get; set; }

        // TODO: parsed JSON filter access with [Computed]

        public string OtcPay { get; set; }

        public string UrlAckPocket { get; set; }

        public string UrlAckPos { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool Performed { get; set; }

        public uint PosId { get; set; }

    }

}
