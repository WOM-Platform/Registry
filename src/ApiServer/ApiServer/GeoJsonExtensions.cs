using System;
using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api {

    public static class GeoJsonExtensions {

        public static bool Contains(this Bounds bbox, GeoJson2DGeographicCoordinates point) {
            bool inLatRange = (bbox.LeftTop.Latitude > bbox.RightBottom.Latitude) ?
                (point.Latitude <= bbox.LeftTop.Latitude && point.Latitude >= bbox.RightBottom.Latitude) :
                (point.Latitude <= bbox.LeftTop.Latitude || point.Latitude >= bbox.RightBottom.Latitude);
            bool inLngRange = (bbox.LeftTop.Longitude < bbox.RightBottom.Longitude) ?
                (point.Longitude >= bbox.LeftTop.Longitude && point.Longitude <= bbox.RightBottom.Longitude) :
                (point.Longitude >= bbox.LeftTop.Longitude || point.Longitude <= bbox.RightBottom.Longitude);

            return inLatRange && inLngRange;
        }

        public static GeoJson2DGeographicCoordinates ToGeoCoord(this double[] vd) {
            if(vd == null) {
                throw new ArgumentNullException(nameof(vd));
            }
            if(vd.Length != 2) {
                throw new ArgumentException("Wrong length of coords vector", nameof(vd));
            }

            if(vd[0] > 90.0 || vd[0] < -90.0) {
                throw new ArgumentException("Latitude cannot be less than -90 or greater than 90");
            }
            if(vd[1] < -180.0 || vd[1] > 180.0) {
                throw new ArgumentException("Longitude cannot be less than -180 or greater than 180");
            }

            return new GeoJson2DGeographicCoordinates(vd[1], vd[0]);
        }

    }

}
