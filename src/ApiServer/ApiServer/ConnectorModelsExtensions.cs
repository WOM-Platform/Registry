using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Connector.Models;

namespace WomPlatform.Web.Api {
    public static class ConnectorModelsExtensions {

        public static DatabaseDocumentModels.Filter ToDocument(this SimpleFilter input) {
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
