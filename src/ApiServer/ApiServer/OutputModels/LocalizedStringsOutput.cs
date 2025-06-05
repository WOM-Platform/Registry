using System.Collections.Generic;
using System.Linq;

namespace WomPlatform.Web.Api.OutputModels {
    public static class LocalizedStringsOutput {
        public static Dictionary<string, string>? ToOutput(this Dictionary<string, string>? input) {
            if(input == null || input.Count == 0) {
                return null;
            }

            // Filter out values that are null or empty
            var output = input.Where(item => !string.IsNullOrWhiteSpace(item.Value)).ToDictionary(item => item.Key, item => item.Value);

            if(output.Count == 0) {
                return null;
            }

            return output;
        }
    }
}
