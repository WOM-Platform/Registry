using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WomPlatform.Web.Api.OutputModels.Aim {
    public class AimListResponse {
        public class AimNode {
            public string Code { get; set; }

            public Dictionary<string, string> Titles { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public AimNode[] Children { get; set; }

            public bool Hidden { get; set; }
        }

        public AimNode[] Aims { get; set; }
    }
}
