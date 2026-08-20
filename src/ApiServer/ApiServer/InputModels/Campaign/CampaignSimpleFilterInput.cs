using MongoDB.Driver.GeoJsonObjectModel;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.InputModels.Campaign {
    public class CampaignSimpleFilterInput : BaseSimpleFilterInput {
    }

    public static class CampaignSimpleFilterInputExtensions {
        public static CampaignSimpleFilter? ToDocument(
            this CampaignSimpleFilterInput? input
        ) {
            if(input == null) {
                return null;
            }

            return new CampaignSimpleFilter {
                SourceId = input.SourceId,
                Aim = input.Aim,
                Bounds = input.Bounds == null ? null : new Bounds {
                    LeftTop = new GeoJson2DGeographicCoordinates(
                        input.Bounds.LeftTop[1],
                        input.Bounds.LeftTop[0]
                    ),
                    RightBottom = new GeoJson2DGeographicCoordinates(
                        input.Bounds.RightBottom[1],
                        input.Bounds.RightBottom[0]
                    )
                },
                Interval = input.Interval == null ? null : new IntervalSpecification {
                    Start = input.Interval.Start,
                    End = input.Interval.End
                }
            };
        }
    }
}
