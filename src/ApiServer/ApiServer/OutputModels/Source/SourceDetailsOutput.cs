using System;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace WomPlatform.Web.Api.OutputModels.Source {
    public class SourceDetailsOutput {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? CreatedOn { get; set; }

        public string PublicKey { get; set; }

        public string PrivateKey { get; set; }

        public class LocationDetails {
            public GeoCoordsOutput Position { get; set; }

            public bool IsFixed { get; set; }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LocationDetails Location { get; set; }
    }

    public static class SourceDetailsOutputExtensions {
        public static SourceDetailsOutput ToDetailsOutput(this DatabaseDocumentModels.Source source) {
            return (source == null) ? null : new SourceDetailsOutput {
                Id = source.Id.ToString(),
                Name = source.Name,
                Url = source.Url,
                CreatedOn = source.CreatedOn,
                PublicKey = source.PublicKey,
                PrivateKey = source.PrivateKey,
                Location = (source.Location == null) ? null : new SourceDetailsOutput.LocationDetails {
                    Position = source.Location.Position.ToOutput(),
                    IsFixed = source.Location.IsFixed,
                }
            };
        }
    }
}
