using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models
{
    public class RedeemPayload
    {
        public Guid nonceId { get; set; }

        public string nonceTs { get; set; }

    }
}
