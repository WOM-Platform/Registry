using System;

namespace WomPlatform.Web.Api.OutputModels {

    [Obsolete("Use GeoCoords")]
    public record Location {
        public double Latitude { get; init; }

        public double Longitude { get; init; }
    }

}
