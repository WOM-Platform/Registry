using MongoDB.Driver.GeoJsonObjectModel;

namespace WomPlatform.Web.Api.InputModels {
    public class GeoCoordsInput {
        public double Latitude { get; init; }

        public double Longitude { get; init; }
    }

    public static class GeoCoordsInputExtensions {
        public static GeoJsonPoint<GeoJson2DGeographicCoordinates> ToGeoJson(this GeoCoordsInput input) {
            if(input == null) {
                return null;
            }

            return GeoJson.Point(new GeoJson2DGeographicCoordinates(input.Longitude, input.Latitude));
        }
    }
}
