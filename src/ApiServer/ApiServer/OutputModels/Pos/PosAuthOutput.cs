namespace WomPlatform.Web.Api.OutputModels.Pos {
    public class PosAuthOutput : PosOutput {
        public string PrivateKey { get; init; }

        public string PublicKey { get; init; }
    }

    public static class PosAuthOutputExtensions {
        public static PosAuthOutput ToAuthOutput(this DatabaseDocumentModels.Pos pos, PictureOutput posCoverPicture) {
            return new PosAuthOutput {
                Id = pos.Id.ToString(),
                MerchantId = pos.MerchantId.ToString(),
                Name = pos.Name,
                Description = pos.Description,
                PrivateKey = pos.PrivateKey,
                PublicKey = pos.PublicKey,
                Latitude = pos.Position?.Coordinates.Latitude,
                Longitude = pos.Position?.Coordinates.Longitude,
                Address = pos.Address.ToOutput(),
                Cover = posCoverPicture,
                Url = pos.Url,
                CreatedOn = pos.CreatedOn,
                LastUpdate = pos.LastUpdate,
                IsActive = pos.IsActive,
            };
        }
    }
}
