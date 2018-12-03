using System;
using System.Text;

namespace WomPlatform.Web.Api {

    public static class StringExtensions {

        public static string ToBase64(this byte[] b) {
            return Convert.ToBase64String(b, Base64FormattingOptions.None);
        }

        public static byte[] FromBase64(this string s) {
            return Convert.FromBase64String(s);
        }

        public static byte[] ToBytes(this string s) {
            return Encoding.UTF8.GetBytes(s);
        }

        public static string AsUtf8String(this byte[] b) {
            return Encoding.UTF8.GetString(b);
        }

    }

}
