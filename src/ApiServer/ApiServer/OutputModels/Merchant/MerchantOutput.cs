using System;
using System.Text.Json.Serialization;
using WomPlatform.Web.Api.OutputModels.Pos;

namespace WomPlatform.Web.Api.OutputModels.Merchant {
    public class MerchantOutput {
        public string Id { get; init; }

        public string Name { get; init; }

        public string FiscalCode { get; init; }

        public MerchantActivityType PrimaryActivity { get; init; }

        public AddressInformation AddressDetails { get; init; }

        [Obsolete]
        public string Address { get; init; }

        [Obsolete]
        public string ZipCode { get; init; }

        [Obsolete]
        public string City { get; init; }

        [Obsolete]
        public string Country { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Description { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Url { get; init; }

        public bool Enabled { get; init; }
    }

    public static class MerchantOutputExtensions {
        public static MerchantOutput ToOutput(this DatabaseDocumentModels.Merchant merchant) {
            return new MerchantOutput {
                Id = merchant.Id.ToString(),
                Name = merchant.Name,
                FiscalCode = merchant.FiscalCode,
                PrimaryActivity = merchant.PrimaryActivityType,
                Address = (merchant.Address?.StreetName).OptConcat(merchant.Address?.StreetNumber),
                ZipCode = merchant.Address?.ZipCode,
                City = merchant.Address?.City,
                Country = merchant.Address?.Country,
                AddressDetails = merchant.Address.ToOutput(),
                Description = merchant.Description,
                Url = merchant.WebsiteUrl,
                Enabled = merchant.Enabled
            };
        }
    }
}
