using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace GuidEncodingConversion {
    internal class MongoDatabase {
        public static string GetCollectionName<T>() {
            if(typeof(T) == typeof(PaymentRequest)) {
                return "PaymentRequests";
            }
            else if(typeof(T) == typeof(GenerationRequest)) {
                return "GenerationRequests";
            }
            else {
                throw new NotSupportedException();
            }
        }
    }
}
