using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public class OperationsTagsAttribute : Attribute {

        public OperationsTagsAttribute(params string[] tags) {
            Tags = tags;
        }

        public string[] Tags { get; set; }

    }

}
