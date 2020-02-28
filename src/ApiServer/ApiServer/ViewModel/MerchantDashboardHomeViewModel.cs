using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.ViewModel {

    public class MerchantDashboardHomeViewModel {

        public string UserFullname { get; set; }

        public string MerchantName { get; set; }

        public class PosViewModel {

            public string Id { get; set; }

            public string Name { get; set; }

        }

        public IEnumerable<PosViewModel> Pos { get; set; }

    }

}
