using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("Vouchers")]
    public class Voucher {

        [Key]
        public uint Id { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public DateTime Timestamp { get; set; }

        public uint SourceId { get; set; }

        public uint? PaymentRequestId { get; set; }

        public uint GenerationRequestId { get; set; }

        public bool Void { get; set; }

    }

}
