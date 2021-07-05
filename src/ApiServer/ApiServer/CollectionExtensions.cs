using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public static class CollectionExtensions {

        /// <summary>
        /// Convert to list or an empty list if null or empty.
        /// </summary>
        public static List<T> ToSafeList<T>(this IEnumerable<T> source) {
            if(source == null)
                return new List<T>();

            return source.ToList();
        }

    }

}
