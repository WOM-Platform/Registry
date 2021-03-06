﻿namespace WomPlatform.Web.Api.OutputModels {

    public record MerchantOutput {

        public string Id { get; init; }
        public string Name { get; init; }
        public string FiscalCode { get; init; }
        public MerchantActivityType PrimaryActivity { get; init; }
        public string Address { get; init; }
        public string ZipCode { get; init; }
        public string City { get; init; }
        public string Country { get; init; }
        public string Description { get; init; }
        public string Url { get; init; }

    }

    public record MerchantAuthOutput : MerchantOutput {

        public PosLoginOutput[] Pos { get; init; }

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
                Url = merchant.WebsiteUrl
            };
        }

    }

}
