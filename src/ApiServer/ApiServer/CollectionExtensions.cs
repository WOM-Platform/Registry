using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        /// <summary>
        /// Converts a list or a null reference to an array.
        /// </summary>
        public static T[] ToSafeArray<T>(this IEnumerable<T> source) {
            if(source == null) {
                return Array.Empty<T>();
            }

            return source.ToArray();
        }

        public static ImmutableDictionary<K, V> ToSafeImmutableDictionary<K, V>(this IDictionary<K, V> d) {
            if(d == null) {
                return ImmutableDictionary<K, V>.Empty;
            }

            return d.ToImmutableDictionary();
        }

        /// <summary>
        /// Returns true if all elements of an enumeration are contained in a list of elements.
        /// </summary>
        public static bool AreAllContainedIn<T>(this IEnumerable<T> source, T[] reference) {
            foreach(var element in source) {
                if(!reference.Contains(element)) {
                    return false;
                }
            }

            return true;
        }

    }

}
