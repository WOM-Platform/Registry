using MongoDB.Driver.GeoJsonObjectModel;

namespace WomPlatform.Web.Api.OutputModels {
    public class GeoCoordsOutput {
        public double Latitude { get; init; }

        public double Longitude { get; init; }
    }

    public static class GeoCoordsOutputExtensions {
        public static GeoCoordsOutput ToOutput(this GeoJsonPoint<GeoJson2DGeographicCoordinates> coords) {
            if(coords == null) {
                return null;
            }

            return new GeoCoordsOutput {
                Latitude = coords.Coordinates.Latitude,
                Longitude = coords.Coordinates.Longitude,
            };
        }
    }
}
