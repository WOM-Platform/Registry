using System.Security.Claims;
using System.Security.Principal;
using MongoDB.Bson;

namespace WomPlatform.Web.Api {

    public class BasicIdentity : GenericIdentity {

        public BasicIdentity(ObjectId userId, string userMail) : base(userMail) {
            AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
            AddClaim(new Claim(ClaimTypes.Email, userMail));
        }

    }

}
