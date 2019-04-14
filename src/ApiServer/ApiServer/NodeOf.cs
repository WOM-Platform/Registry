using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public class NodeOf<T> : Tuple<T, IList<NodeOf<T>>> {
        public NodeOf(T item, IList<NodeOf<T>> children)
            : base(item, children) {
        }
    }

}
