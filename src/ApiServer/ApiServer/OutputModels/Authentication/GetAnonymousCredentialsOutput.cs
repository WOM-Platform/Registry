using MongoDB.Bson;

namespace WomPlatform.Web.Api.OutputModels.Authentication {
    public class GetAnonymousCredentialsOutput {
        public ObjectId PosId { get; init; }

        public string PosPrivateKey { get; init; }
    }
}
