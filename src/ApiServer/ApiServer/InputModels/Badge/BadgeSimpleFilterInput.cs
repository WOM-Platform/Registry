using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;

namespace WomPlatform.Web.Api.InputModels.Badge {
    public class BadgeSimpleFilterInput {
        [Required]
        [Range(1, int.MaxValue)]
        public int Count { get; set; }

        /// <summary>
        /// Optional source ID filter.
        /// </summary>
        public ObjectId? SourceId { get; set; }

        /// <summary>
        /// Optional aim filter, expressed as an aim code (prefix).
        /// </summary>
        public string? Aim { get; set; }

        /// <summary>
        /// Optional geographical bounds filter.
        /// </summary>
        public GeoBoundsInput? Bounds { get; set; }

        /// <summary>
        /// Optional time interval filter.
        /// </summary>
        public IntervalSpecificationInput? Interval { get; set; }
    }

    public static class BadgeSimpleFilterInputExtensions {
        public static DatabaseDocumentModels.BadgeSimpleFilter ToDocument(this BadgeSimpleFilterInput input) {
            if(input == null) {
                return null;
            }

            return new DatabaseDocumentModels.BadgeSimpleFilter {
                Count = input.Count,
                SourceId = input.SourceId,
                Aim = input.Aim,
                Bounds = input.Bounds == null ? null : new DatabaseDocumentModels.Bounds {
                    LeftTop = new GeoJson2DGeographicCoordinates(input.Bounds.LeftTop[1], input.Bounds.LeftTop[0]),
                    RightBottom = new GeoJson2DGeographicCoordinates(input.Bounds.RightBottom[1], input.Bounds.RightBottom[0]),
                },
                Interval = input.Interval == null ? null : new DatabaseDocumentModels.IntervalSpecification {
                    Start = input.Interval.Start,
                    End = input.Interval.End,
                }
            };
        }
    }
}
