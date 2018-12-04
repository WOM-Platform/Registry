using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public static class UrlGenerator {

        private const string RootUrl = "https://wom.social";
        private const string ApiPath = "api/v1";

        public static string GenerateRedeemUrl(Guid otcGen) {
            return string.Format("{0}/redeem/{1:N}", RootUrl, otcGen);
        }

        public static string GenerateSourceUrl(uint sourceId) {
            return string.Format("{0}/sources/{1}", RootUrl, sourceId);
        }

    }

}
