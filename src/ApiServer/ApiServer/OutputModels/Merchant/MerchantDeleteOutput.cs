namespace WomPlatform.Web.Api.OutputModels.Merchant {
    public class MerchantDeleteOutput {
        public bool OperationPerformed { get; set; }

        public long CountOfDeletedMerchants { get; set; }

        public long CountOfDeletedPos { get; set; }

        public long CountOfDeletedOffers { get; set; }
    }
}
