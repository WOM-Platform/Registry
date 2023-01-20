using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WomPlatform.Web.Api.OutputModels.Pos {
    public class PosOutput {
        public string Id { get; init; }

        public string Name { get; init; }

        public string Description { get; init; }

        public double? Latitude { get; init; }

        public double? Longitude { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Url { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PictureOutput Cover { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? CreateOn { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? LastUpdate { get; init; }

        public bool IsActive { get; init; }
    }

    public static class PosOutputExtensions {
        public static PosOutput ToOutput(this DatabaseDocumentModels.Pos pos, PictureOutput posCoverPicture) {
            return new PosOutput {
                Id = pos.Id.ToString(),
                Name = pos.Name,
                Description = pos.Description,
                Latitude = pos.Position.Coordinates.Latitude,
                Longitude = pos.Position.Coordinates.Longitude,
                Cover = posCoverPicture,
                Url = pos.Url,
                CreateOn = pos.CreatedOn,
                LastUpdate = pos.LastUpdate,
                IsActive = pos.IsActive,
            };
        }
    }
}
