using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using WomPlatform.Web.Api.DatabaseModels;

namespace WomPlatform.Web.Api {

    public class WomUserIdentity : GenericIdentity {

        private readonly User _user;

        public WomUserIdentity(User user) : base(user.Username, "WOM user") {
            _user = user;
        }

        public User WomUser => _user;

    }

}
