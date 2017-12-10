using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models
{
    public class RegisterResponse
    {
        public int PosId { get; set; }

        public string OTCpay { get;  set; }

        public Guid nonceId { get; set; }

        public string nonceTs { get; set; }
    }
}
