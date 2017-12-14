using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels
{
    public class Voucher
    {
        public int Id { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string CreatedAt { get; set; }
        public string Type { get; set; }
        public string OTC { get; set; }
        public string Nonce { get; set; }
        public int PaymentRequest_Id { get; set; }
        public int Source_Id { get; set; }
        public int GenerationRequest_Id { get; set; }
    }
}
