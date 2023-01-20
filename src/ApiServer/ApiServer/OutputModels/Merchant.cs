using System.Text.Json.Serialization;
using WomPlatform.Web.Api.OutputModels.Pos;

namespace WomPlatform.Web.Api.OutputModels {

    public record MerchantOutput {
        public string Id { get; init; }

        public string Name { get; init; }

        public string FiscalCode { get; init; }

        public MerchantActivityType PrimaryActivity { get; init; }

        public string Address { get; init; }

        public string ZipCode { get; init; }

        public string City { get; init; }

        public string Country { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Description { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Url { get; init; }

        public bool Enabled { get; init; }
    }

    public record MerchantAuthOutput : MerchantOutput {
        public PosAuthOutput[] Pos { get; init; }
    }

    public static class MerchantOutputHelpers {

        public static MerchantOutput ToOutput(this DatabaseDocumentModels.Merchant merchant) {
            return new MerchantOutput {
                Id = merchant.Id.ToString(),
                Name = merchant.Name,
                FiscalCode = merchant.FiscalCode,
                PrimaryActivity = merchant.PrimaryActivityType,
                Address = merchant.Address,
                ZipCode = merchant.ZipCode,
                City = merchant.City,
                Country = merchant.Country,
                Description = merchant.Description,
                Url = merchant.WebsiteUrl,
                Enabled = merchant.Enabled
            };
        }

    }

}
