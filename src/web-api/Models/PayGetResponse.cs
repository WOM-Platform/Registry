using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models
{
    public class PayGetResponse
    {
        public int amount { get; set; }
        
        //filter json
        public FilterInfo filter { get; set; }

        public PosInfo POS { get; set; }
    }
}
