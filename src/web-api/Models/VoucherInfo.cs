using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models
{
    public class VoucherInfo
    {
        public int VoucherID { get; set; }

        public int Latitude { get; set; }

        public int Longitude { get; set; }

        public int Timestamp { get; set; }

        public string Type { get; set; }
    }
}
