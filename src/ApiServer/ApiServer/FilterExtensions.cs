using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api {

    public static class FilterExtensions {

        public static Bounds GetBounds(this Connector.Models.SimpleFilter simpleFilter) {
            if(simpleFilter.Bounds == null) {
                return null;
            }

            return new Bounds {
                LeftTop = new GeoJson2DGeographicCoordinates(simpleFilter.Bounds.LeftTop[1], simpleFilter.Bounds.LeftTop[0]),
                RightBottom = new GeoJson2DGeographicCoordinates(simpleFilter.Bounds.RightBottom[1], simpleFilter.Bounds.RightBottom[0])
            };
        }

        public static Connector.Models.Bounds ToBounds(this Bounds bbox) {
            if(bbox == null) {
                return null;
            }

            return new Connector.Models.Bounds {
                LeftTop = new double[] {
                    bbox.LeftTop.Latitude, bbox.LeftTop.Longitude
                },
                RightBottom = new double[] {
                    bbox.RightBottom.Latitude, bbox.RightBottom.Longitude
                }
            };
        }

        public static Connector.Models.SimpleFilter ToSimpleFilter(this Filter filter) {
            if(filter == null) {
                return null;
            }

            return new Connector.Models.SimpleFilter {
                Aim = filter.Aims,
                Bounds = filter.Bounds.ToBounds(),
                MaxAge = filter.MaxAge
            };
        }

    }

}
