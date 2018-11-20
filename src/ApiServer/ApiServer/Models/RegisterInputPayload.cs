using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models
{
    public class RegisterInputPayload
    {
        public int PosId { get; set; }

        public Guid nonceId { get; set; }

        public int nonceTs { get; set; }

        public FilterInfo Filter { get; set; }

        public int amount { get; set; }

        public string ackPocket { get; set; }

        public string ackPos { get; set; }

        public bool permanent { get; set; }
    }
}
