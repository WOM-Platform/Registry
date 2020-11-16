using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.ViewModel {

    public class LoginViewModel {

        public bool PreviousLoginFailed { get; set; } = false;

        public bool HasResetPassword { get; set; } = false;

        public string Username { get; set; }

        public string ReturnUrl { get; set; }

    }

}
