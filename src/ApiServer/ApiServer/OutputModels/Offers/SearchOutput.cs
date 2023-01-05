using System;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.OutputModels.Offers {
    public class OfferSearchPosOutput {
        public string PosId { get; init; }

        public string Name { get; init; }

        public string Description { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PictureOutput Cover { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Url { get; init; }

        public GeoCoords Position { get; init; }

        public OfferSearchOfferOutput[] Offers { get; init; }
    }

    public class OfferSearchOfferOutput {
        public ObjectId OfferId { get; init; }

        public string Title { get; init; }

        public string Description { get; init; }

        public int Cost { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SimpleFilter Filter { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime UpdatedAt { get; init; }
    }
}
