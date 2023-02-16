﻿using System;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.OutputModels.Offers {
    public class OfferOutput {
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
            public string RegistryUrl { get; set; }

            public Guid Otc { get; set; }

            public string Password { get; set; }

            public string Link { get; set; }
        }

        public PaymentDetails Payment { get; set; }
    }

    public static class OfferOutputExtensions {

        public static OfferOutput ToDetailsOutput(this DatabaseDocumentModels.Offer offer) {
            var selfHostDomain = Environment.GetEnvironmentVariable("SELF_HOST");
            var selfLinkDomain = Environment.GetEnvironmentVariable("LINK_HOST");

            return offer == null ? null : new OfferOutput {
                Id = offer.Id,
                Title = offer.Title,
                Description = offer.Description,
                Cost = offer.Payment.Cost,
                Filter = offer.Payment.Filter.ToOutput(),
                CreatedOn = offer.CreatedOn,
                LastUpdate = offer.LastUpdate,
                Deactivated = offer.Deactivated,
                Payment = new OfferOutput.PaymentDetails {
                    RegistryUrl = $"https://{selfHostDomain}",
                    Otc = offer.Payment.Otc,
                    Password = offer.Payment.Password,
                    Link = $"https://{selfLinkDomain}/payment/{offer.Payment.Otc:D}",
                },
            };
        }

    }
}
