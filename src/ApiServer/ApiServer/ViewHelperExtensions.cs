using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace WomPlatform.Web.Api {

    public static class ViewHelperExtensions {

        public const string WebsiteTitle = "Worth One Minute";

        public static string ToTitle(this ViewDataDictionary<dynamic> viewData, string key) {
            var title = viewData[key] as string;
            if(string.IsNullOrWhiteSpace(title)) {
                return WebsiteTitle;
            }
            else {
                return string.Format("{0} | {1}", WebsiteTitle, title);
            }
        }

    }

}
