using WomPlatform.Web.Api.InputModels;

namespace WomPlatform.Web.Api.OutputModels.Badge {
    public class BadgeSimpleFilterOutput {
        public int Count { get; set; }

        /// <summary>
        /// Optional source ID filter.
        /// </summary>
        public string? SourceId { get; set; }

        /// <summary>
        /// Optional aim filter, expressed as an aim code (prefix).
        /// </summary>
        public string? Aim { get; set; }

        /// <summary>
        /// Optional geographical bounds filter.
        /// </summary>
        public GeoBoundsOutput? Bounds { get; set; }

        /// <summary>
        /// Optional time interval filter.
        /// </summary>
        public IntervalSpecificationOutput? Interval { get; set; }
    }

    public static class BadgeSimpleFilterOutputExtensions {
        public static BadgeSimpleFilterOutput ToOutput(this DatabaseDocumentModels.BadgeSimpleFilter? badgeSimpleFilter) {
            if(badgeSimpleFilter == null) {
                return null;
            }

            return new BadgeSimpleFilterOutput {
                Count = badgeSimpleFilter.Count,
                SourceId = badgeSimpleFilter.SourceId?.ToString(),
                Aim = badgeSimpleFilter.Aim,
                Bounds = badgeSimpleFilter.Bounds?.ToOutput(),
                Interval = badgeSimpleFilter.Interval?.ToOutput(),
            };
        }
    }
}
