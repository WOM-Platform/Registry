﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.ViewModel {

    public class LoginStatusViewModel {

        public bool IsLoggedIn { get; set; }

        public string UserIdentifier { get; set; }

        public string FullName { get; set; }

    }

}
