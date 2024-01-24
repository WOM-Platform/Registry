using System.Collections.Generic;
using WomPlatform.Web.Api.OutputModels.Pos;

namespace WomPlatform.Web.Api.OutputModels.Merchant {
    public class MerchantAuthOutput : MerchantOutput {
        public PosAuthOutput[] Pos { get; init; }

        public MerchantRole Access { get; init; }
    }

    public static class MerchantAuthOutputExtensions {
        public static MerchantAuthOutput ToAuthOutput(
            this DatabaseDocumentModels.Merchant merchant,
            IEnumerable<PosAuthOutput> pos,
            MerchantRole access
        ) {
            return new MerchantAuthOutput {
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
                Enabled = merchant.Enabled,

                Pos = pos.ToSafeArray(),
                Access = access,
            };
        }
    }
}
