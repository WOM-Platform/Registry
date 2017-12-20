using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels
{
    public class Aim
    {
        public long Id { get; set; }
        public string Description { get; set; }
        public long ContactId { get; set; }
        public string Type { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
