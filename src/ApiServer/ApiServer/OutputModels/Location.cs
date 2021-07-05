using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.OutputModels {

    public record Location {

        public double Latitude { get; init; }
        public double Longitude { get; init; }

    }

}
