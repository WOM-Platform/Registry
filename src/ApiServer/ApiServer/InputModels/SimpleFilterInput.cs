using MongoDB.Driver.GeoJsonObjectModel;

namespace WomPlatform.Web.Api.InputModels {
    public class SimpleFilterInput {
        /// <summary>
        /// Optional aim filter, expressed as an aim code (prefix).
        /// </summary>
        public string? Aim { get; set; }

        /// <summary>
        /// Optional geographical bounds filter.
        /// </summary>
        public GeoBoundsInput? Bounds { get; set; }

        /// <summary>
        /// Optional voucher freshness filter, expressed in days of age.
        /// </summary>
        public long? MaxAge { get; set; }
    }

    public static class SimpleFilterInputExtensions {
        public static DatabaseDocumentModels.Filter ToDocument(this SimpleFilterInput input) {
            return input == null ? null : new DatabaseDocumentModels.Filter {
                Aims = input.Aim,
                Bounds = input.Bounds == null ? null : new DatabaseDocumentModels.Bounds {
                    LeftTop = new GeoJson2DGeographicCoordinates(input.Bounds.LeftTop[1], input.Bounds.LeftTop[0]),
                    RightBottom = new GeoJson2DGeographicCoordinates(input.Bounds.RightBottom[1], input.Bounds.RightBottom[0]),
                },
                MaxAge = input.MaxAge,
            };
        }
    }
}
