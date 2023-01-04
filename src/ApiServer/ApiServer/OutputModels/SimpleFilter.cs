namespace WomPlatform.Web.Api.OutputModels {
    public class SimpleFilter {
        /// <summary>
        /// Optional aim filter, expressed as an aim code (prefix).
        /// </summary>
        public string Aim { get; set; }

        /// <summary>
        /// Optional geographical bounds filter.
        /// </summary>
        public GeoBounds Bounds { get; set; }

        /// <summary>
        /// Optional voucher freshness filter, expressed in days of age.
        /// </summary>
        public long? MaxAge { get; set; }
    }

    public static class SimpleFilterExtensions {
        public static SimpleFilter ToOutput(this DatabaseDocumentModels.Filter filter) {
            return (filter == null || (filter.Aims == null && filter.Bounds == null && filter.MaxAge == null)) ? null : new SimpleFilter {
                Aim = filter.Aims,
                Bounds = filter.Bounds.ToOutput(),
                MaxAge = filter.MaxAge,
            };
        }
    }
}
