using System;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.DTO {
    public class MerchantReportDto {
        [CsvHelper.Configuration.Attributes.Ignore]
        public ObjectId MerchantId { get; set; }

        [CsvHelper.Configuration.Attributes.Name("MerchantId")]
        public string MerchantIdString {
            get {
                return MerchantId.ToString();
            }
        }

        public string MerchantName { get; set; }

        public string MerchantFiscalCode { get; set; }

        public DateTime MerchantLastUpdate { get; set; }

        public string UserEmail { get; set; }

        public string UserName { get; set; }

        public string UserSurname { get; set; }

        public string OfferTitle { get; set; }

        public int OfferCost { get; set; }

        public DateTime OfferCreation { get; set; }

        public DateTime OfferLastUpdate { get; set; }
    }
}
