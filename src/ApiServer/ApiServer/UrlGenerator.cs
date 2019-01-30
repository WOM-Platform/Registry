using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public static class UrlGenerator {

        private const string RootUrl = "https://wom.social";
        private const string ApiPath = "api/v1";

        public static string GenerateRedeemUrl(Guid otcGen) {
            return string.Format("{0}/vouchers/{1:N}", RootUrl, otcGen);
        }

        public static string GeneratePaymentUrl(Guid otcPay) {
            return string.Format("{0}/payment/{1:N}", RootUrl, otcPay);
        }

        public static string GenerateSourceUrl(uint sourceId) {
            return string.Format("{0}/{1}/source/{2}", RootUrl, ApiPath, sourceId);
        }

        public static string GenerateAimUrl(string code) {
            return string.Format("{0}/{1}/aim/{2}", RootUrl, ApiPath, code);
        }

    }

}
