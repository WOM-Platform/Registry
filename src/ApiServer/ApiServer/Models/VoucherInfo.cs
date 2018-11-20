using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models
{
    public class VoucherInfo
    {
        public int VoucherID { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public DateTime Timestamp { get; set; }

        public string Type { get; set; }
    }
}
