using System.Text.Json.Serialization;

namespace WomPlatform.Web.Api.OutputModels {

    public record PosOutput {
        public string Id { get; init; }

        public string Name { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Latitude { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Longitude { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Url { get; init; }

        public bool IsActive { get; init; }
    }

    public record PosLoginOutput : PosOutput {
        public string PrivateKey { get; init; }

        public string PublicKey { get; init; }
    }

    public static class PosOutputHelpers {
        public static PosOutput ToOutput(this DatabaseDocumentModels.Pos pos) {
            return new PosOutput {
                Id = pos.Id.ToString(),
                Name = pos.Name,
                Latitude = pos.Position?.Coordinates?.Latitude,
                Longitude = pos.Position?.Coordinates?.Longitude,
                Url = pos.Url,
                IsActive = pos.IsActive,
            };
        }

        public static PosLoginOutput ToLoginOutput(this DatabaseDocumentModels.Pos pos) {
            return new PosLoginOutput {
                Id = pos.Id.ToString(),
                Name = pos.Name,
                Latitude = pos.Position?.Coordinates?.Latitude,
                Longitude = pos.Position?.Coordinates?.Longitude,
                Url = pos.Url,
                IsActive = pos.IsActive,
                PrivateKey = pos.PrivateKey,
                PublicKey = pos.PublicKey,
            };
        }
    }

}
