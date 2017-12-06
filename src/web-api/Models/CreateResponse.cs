using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models
{
    public class CreateResponse
    {
        public Guid Id { get; set; }

        public string OtcGen { get; set; }

        public string Timestamp { get; set; }

        //le informazioni relative ai voucher vengono gestite dalla classe VoucherInfo
        public VoucherInfo[] nVoucher {get;set; }
        
    }
}
