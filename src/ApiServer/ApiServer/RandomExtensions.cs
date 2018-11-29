using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public static class RandomExtensions {

        public static double NextBetween(this Random r, double min, double max) {
            var @base = Math.Min(min, max);
            var range = Math.Max(min, max) - @base;

            return @base + r.NextDouble() * range;
        }

    }

}
