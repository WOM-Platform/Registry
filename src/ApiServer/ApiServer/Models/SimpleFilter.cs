using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.Models {

    public class SimpleFilter {

        public string Aim { get; set; }

        public Bounds GeoBounds { get; set; }

        public long MaxAge { get; set; }

    }

}
