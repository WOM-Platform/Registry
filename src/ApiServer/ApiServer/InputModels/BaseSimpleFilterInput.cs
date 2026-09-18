using MongoDB.Bson;

namespace WomPlatform.Web.Api.InputModels {
    public class BaseSimpleFilterInput {

        /// <summary>
        ///     Optional source ID filter.
        /// </summary>
        public ObjectId? SourceId { get; set; }

        /// <summary>
        ///     Optional aim filter, expressed as an aim code (prefix).
        /// </summary>
        public string? Aim { get; set; }

        /// <summary>
        ///     Optional geographical bounds filter.
        /// </summary>
        public GeoBoundsInput? Bounds { get; set; }

        /// <summary>
        ///     Optional time interval filter.
        /// </summary>
        public IntervalSpecificationInput? Interval { get; set; }
    }
}
