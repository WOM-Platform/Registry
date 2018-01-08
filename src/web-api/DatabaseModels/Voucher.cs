using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels {

    public class Voucher {

        [ExplicitKey]
        public Guid Id { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public DateTime Timestamp { get; set; }

        public string OTC { get; set; }

        public string Type { get; set; }

        public Guid Nonce { get; set; }

        public long SourceId { get; set; }

        public long? PaymentRequestId { get; set; }

        public long GenerationRequestId { get; set; }

    }

}
