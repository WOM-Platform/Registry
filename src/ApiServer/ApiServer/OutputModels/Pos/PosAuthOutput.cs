using System.ComponentModel;
using System.Text.Json.Serialization;
using System;

namespace WomPlatform.Web.Api.OutputModels.Pos {
    public class PosAuthOutput {
        public string Id { get; init; }

        public string Name { get; init; }

        public string Description { get; init; }

        public string PrivateKey { get; init; }

        public string PublicKey { get; init; }

        public double? Latitude { get; init; }

        public double? Longitude { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Url { get; init; }
        
        [DefaultValue(true)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool IsActive { get; init; } = true;
    }

    public static class PosAuthOutputExtensions {
        public static PosAuthOutput ToAuthOutput(this DatabaseDocumentModels.Pos pos) {
            return new PosAuthOutput {
                Id = pos.Id.ToString(),
                Name = pos.Name,
                Description = pos.Description,
                PrivateKey = pos.PrivateKey,
                PublicKey = pos.PublicKey,
                Latitude = pos.Position.Coordinates.Latitude,
                Longitude = pos.Position.Coordinates.Longitude,
                Url = pos.Url,
                IsActive = pos.IsActive,
            };
        }
    }
}
