using System.Text.Json.Serialization;
using System;

namespace WomPlatform.Web.Api.OutputModels.Source {
    public class SourceOutput {
        public SourceOutput(DatabaseDocumentModels.Source source) {
            Id = source.Id.ToString();
            Name = source.Name;
            Url = source.Url;
            CreatedOn = source.CreatedOn;
            Location = (source.Location == null) ? null : new SourceOutput.LocationDetails {
                Position = source.Location.Position.ToOutput(),
                IsFixed = source.Location.IsFixed,
            };
            DefaultLocation = (source.Location == null) ? null : source.Location.Position.ToOutput();
            LocationIsFixed = source.Location?.IsFixed ?? false;
        }

        public string Id { get; init; }

        public string Name { get; init; }

        public string Url { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? CreatedOn { get; init; }

        public class LocationDetails {
            public GeoCoordsOutput Position { get; set; }

            public bool IsFixed { get; set; }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LocationDetails Location { get; set; }

        [Obsolete]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeoCoordsOutput DefaultLocation { get; init; }

        [Obsolete]
        public bool LocationIsFixed { get; init; }
    }
}
