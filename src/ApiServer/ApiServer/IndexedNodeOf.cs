using System;
using System.Collections.Generic;
using WomPlatform.Web.Api.DatabaseModels;

namespace WomPlatform.Web.Api {

    public class IndexedNodeOf<K, T> : Tuple<T, IDictionary<K, IndexedNodeOf<K, T>>> {

        public IndexedNodeOf(T item, IDictionary<K, IndexedNodeOf<K, T>> children)
            : base(item, children) {
        }

        public T Item => Item1;

        public IDictionary<K, IndexedNodeOf<K, T>> Children => Item2;

    }

}
