using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels
{
    public class POS
    {

        public long Id { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public DateTime CreationDate { get; set; }
        public string URLPOS { get; set; }
    }
}
