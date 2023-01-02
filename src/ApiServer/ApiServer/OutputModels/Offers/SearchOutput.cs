using System;
using System.Text.Json.Serialization;

namespace WomPlatform.Web.Api.OutputModels.Offers {
    public class OfferSearchPosOutput {
        public string PosId { get; init; }

        public string Name { get; init; }

        public string Description { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PictureOutput Picture { get; init; }

        public string Url { get; init; }

        public GeoCoords Position { get; init; }

        public double DistanceInKms { get; init; }

        public OfferSearchOfferOutput[] Offers { get; init; }
    }

    public class OfferSearchOfferOutput {
        public Guid OfferId { get; init; }

        public string Title { get; init; }

        public string Description { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PictureOutput Picture { get; init; }

        public int Cost { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime UpdatedAt { get; init; }
    }
}
