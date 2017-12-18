using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels
{
    public class GenerationRequest
    {
        public int Id { get; set; }
        public int Source_Id { get; set; }
        public int Amount { get; set; }
        public string OTC { get; set; }
        public string CreatedAt { get; set; }
        public string State { get; set; }
    }
}
