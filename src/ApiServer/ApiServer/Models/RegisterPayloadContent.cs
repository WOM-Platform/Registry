using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models
{
    public class RegisterPayloadContent
    {
        
        public int PosId { get; set; }

        public RegisterInputPayload Payload { get; set; }
    }
}

