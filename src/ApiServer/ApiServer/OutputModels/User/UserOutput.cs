using MongoDB.Bson;

namespace WomPlatform.Web.Api.OutputModels.User {
    public class UserOutput {
        public ObjectId Id { get; set; }

        public string Email { get; set; }

        public string Name { get; set; }

        public string Surname { get; set; }

        public bool Verified { get; set; }
    }

    public static class UserOutputExtensions {
        public static UserOutput ToOutput(this DatabaseDocumentModels.User user, bool conceal) {
            return new UserOutput {
                Id = user.Id,
                Email = conceal ? user.Email.ConcealEmail() : user.Email,
                Name = user.Name,
                Surname = conceal ? user.Surname.Conceal() : user.Surname,
                Verified = user.VerificationToken == null,
            };
        }
    }
}
