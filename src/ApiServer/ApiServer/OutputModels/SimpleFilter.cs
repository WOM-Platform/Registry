using System.Text.Json.Serialization;

namespace WomPlatform.Web.Api.OutputModels {
    public class SimpleFilter {
        /// <summary>
        /// Optional aim filter, expressed as an aim code (prefix).
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Aim { get; set; }

        /// <summary>
        /// Optional geographical bounds filter.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public GeoBoundsOutput Bounds { get; set; }

        /// <summary>
        /// Optional voucher freshness filter, expressed in days of age.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long? MaxAge { get; set; }
    }

    public static class SimpleFilterExtensions {
        public static SimpleFilter ToOutput(this DatabaseDocumentModels.Filter? filter) {
            if(filter == null) {
                return null;
            }

            if(filter.Aims == null && filter.Bounds == null && filter.MaxAge == null) {
                return null;
            }

            return new SimpleFilter {
                Aim = filter.Aims,
                Bounds = filter.Bounds.ToOutput(),
                MaxAge = filter.MaxAge,
            };
        }
    }
}
