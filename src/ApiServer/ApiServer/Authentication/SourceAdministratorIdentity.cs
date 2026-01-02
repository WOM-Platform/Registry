using System.Security.Claims;
using System.Security.Principal;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Authentication {
    public class SourceAdministratorIdentity : GenericIdentity {
        public SourceAdministratorIdentity(Source source) : base(source.Id.ToString()) {
            AddClaim(new Claim(RegistryClaims.Role, PlatformRole.User.ToString()));
            AddClaim(new Claim(RegistryClaims.SourceId, source.Id.ToString()));
        }
    }
}
