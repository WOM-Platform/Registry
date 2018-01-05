using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels
{
    public class GenerationRequest
    {
        public long Id { get; set; }
        public long Source_Id { get; set; }
        public int Amount { get; set; }
        public string OTC { get; set; }
        public DateTime CreatedAt { get; set; }
        public string State { get; set; }
    }
}
