using System;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.OutputModels.Offers {
    public class PosOfferOutput {
        public ObjectId Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public int Cost { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public SimpleFilter Filter { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime LastUpdate { get; set; }

        public bool Deactivated { get; set; }
    }

    public static class OfferOfPosOutputExtensions {
        public static PosOfferOutput ToOutput(this DatabaseDocumentModels.Offer offer) {
            return offer == null ? null : new PosOfferOutput {
                Id = offer.Id,
                Title = offer.Title,
                Description = offer.Description,
                Cost = offer.Cost,
                Filter = offer.Filter.ToOutput(),
                CreatedOn = offer.CreatedOn,
                LastUpdate = offer.LastUpdate,
                Deactivated = offer.Deactivated,
            };
        }
    }
}
