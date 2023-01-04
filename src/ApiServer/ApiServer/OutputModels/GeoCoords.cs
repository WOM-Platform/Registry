using MongoDB.Driver.GeoJsonObjectModel;

namespace WomPlatform.Web.Api.OutputModels {

    public class GeoCoords {
        public double Latitude { get; init; }

        public double Longitude { get; init; }
    }

    public static class GeoCoordsExtensions {

        public static GeoCoords ToOutput(this GeoJsonPoint<GeoJson2DGeographicCoordinates> coords) {
            return new GeoCoords {
                Latitude = coords.Coordinates.Latitude,
                Longitude = coords.Coordinates.Longitude,
            };
        }

    }

}
