using System.Security.Principal;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api {

    public class WomUserIdentity : GenericIdentity {

        public WomUserIdentity(User user) : base(user.Email, "WOM user") {
            WomUser = user;
        }

        public User WomUser { get; }

    }

}
