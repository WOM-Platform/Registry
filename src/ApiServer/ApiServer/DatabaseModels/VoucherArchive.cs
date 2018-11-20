using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels
{
    public class VoucherArchive
    {
        public long Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; }
        public string OTC { get; set; }
        public string Nonce { get; set; }
        public long PaymentRequest_Id { get; set; }
        public long Source_Id { get; set; }
        public long GenerationRequest_Id { get; set; }
    }
}
