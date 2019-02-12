using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public struct GeoBounds {

        public GeoCoords LeftTop { get; set; }

        public GeoCoords RightBottom { get; set; }

    }

    public static class GeoBoundsExtensions {

        /// <summary>
        /// Checks whether a geographic point is within geographic bounds.
        /// Considers bounds spanning around the geosphere.
        /// </summary>
        public static bool Contains(this GeoBounds bounds, GeoCoords point) {
            bool inLatRange = (bounds.LeftTop.Latitude > bounds.RightBottom.Latitude) ?
                (point.Latitude <= bounds.LeftTop.Latitude && point.Latitude >= bounds.RightBottom.Latitude) :
                (point.Latitude <= bounds.LeftTop.Latitude || point.Latitude >= bounds.RightBottom.Latitude);
            bool inLngRange = (bounds.LeftTop.Longitude < bounds.RightBottom.Longitude) ?
                (point.Longitude >= bounds.LeftTop.Longitude && point.Longitude <= bounds.RightBottom.Longitude) :
                (point.Longitude >= bounds.LeftTop.Longitude || point.Longitude <= bounds.RightBottom.Longitude);

            return inLatRange && inLngRange;
        }

    }

}
