using System;
using System.Text.Json.Serialization;

namespace WomPlatform.Web.Api.OutputModels.VersionCheck {
    public class VersionCheckResponse {

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum UpdateStatus {
            Ok,
            ShouldUpdate,
            MustUpdate
        }

        public UpdateStatus Status { get; init; }

        [JsonConverter(typeof(JsonStringVersionConverter))]
        public Version LatestVersion { get; init; } = null;
    }
}
