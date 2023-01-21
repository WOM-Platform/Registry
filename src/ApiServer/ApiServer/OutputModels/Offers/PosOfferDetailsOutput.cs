using System;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.OutputModels.Offers {
    public class PosOfferDetailsOutput {
        public ObjectId Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public int Cost { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public SimpleFilter Filter { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime LastUpdate { get; set; }

        public bool Deactivated { get; set; }

        public class PaymentDetails {
            public Guid Otc { get; set; }

            public string Password { get; set; }
        }

        public PaymentDetails Payment { get; set; }
    }

    public static class PosOfferDetailsOutputExtensions {

        public static PosOfferDetailsOutput ToDetailsOutput(this DatabaseDocumentModels.Offer offer, DatabaseDocumentModels.PaymentRequest paymentRequest) {
            return offer == null ? null : new PosOfferDetailsOutput {
                Id = offer.Id,
                Title = offer.Title,
                Description = offer.Description,
                Cost = offer.Cost,
                Filter = offer.Filter.ToOutput(),
                CreatedOn = offer.CreatedOn,
                LastUpdate = offer.LastUpdate,
                Deactivated = offer.Deactivated,
                Payment = new PosOfferDetailsOutput.PaymentDetails {
                    Otc = paymentRequest.Otc,
                    Password = paymentRequest.Password,
                },
            };
        }

    }
}
