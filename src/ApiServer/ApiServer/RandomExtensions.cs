using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public static class RandomExtensions {

        public static double NextBetween(this Random r, double min, double max) {
            var @base = Math.Min(min, max);
            var range = Math.Max(min, max) - @base;

            return @base + r.NextDouble() * range;
        }

        public static T Pick<T>(this Random r, IList<T> source) {
            int c = source.Count;
            return source[r.Next(c)];
        }

        public static string GeneratePassword(this Random r, int length) {
            var sb = new StringBuilder(length);
            for(int i = 0; i < length; ++i) {
                sb.Append(r.Next(10).ToString());
            }
            return sb.ToString();
        }

    }

}
