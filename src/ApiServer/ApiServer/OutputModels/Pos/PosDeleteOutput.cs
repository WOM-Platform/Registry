namespace WomPlatform.Web.Api.OutputModels.Pos {
    public class PosDeleteOutput {
        public bool OperationPerformed { get; set; }

        public long CountOfDeletedPos { get; set; }

        public long CountOfDeletedOffers { get; set; }
    }
}
