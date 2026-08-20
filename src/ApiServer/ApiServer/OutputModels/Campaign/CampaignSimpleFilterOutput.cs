using WomPlatform.Web.Api.DatabaseDocumentModels;
using WomPlatform.Web.Api.InputModels;

namespace WomPlatform.Web.Api.OutputModels.Campaign {
    public class CampaignSimpleFilterOutput {
        /// <summary>
        ///     Optional source ID filter.
        /// </summary>
        public string? SourceId { get; set; }

        /// <summary>
        ///     Optional aim filter, expressed as an aim code (prefix).
        /// </summary>
        public string? Aim { get; set; }

        /// <summary>
        ///     Optional geographical bounds filter.
        /// </summary>
        public GeoBoundsOutput? Bounds { get; set; }

        /// <summary>
        ///     Optional time interval filter.
        /// </summary>
        public IntervalSpecificationOutput? Interval { get; set; }
    }

    public static class CampaignSimpleFilterOutputExtensions {
        public static CampaignSimpleFilterOutput? ToOutput(
            this CampaignSimpleFilter? campaignSimpleFilter
        ) {
            if(campaignSimpleFilter == null) {
                return null;
            }

            return new CampaignSimpleFilterOutput {
                SourceId = campaignSimpleFilter.SourceId?.ToString(),
                Aim = campaignSimpleFilter.Aim,
                Bounds = campaignSimpleFilter.Bounds?.ToOutput(),
                Interval = campaignSimpleFilter.Interval?.ToOutput()
            };
        }
    }
}
