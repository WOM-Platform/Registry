using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    /// <summary>
    /// Groups operations with tags, for Swagger output.
    /// </summary>
    class OperationsTagsAttribute : Attribute {

        public OperationsTagsAttribute(params string[] tags) {
            Tags = tags;
        }

        public string[] Tags { get; set; }

    }

}
