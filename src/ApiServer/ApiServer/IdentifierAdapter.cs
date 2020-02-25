using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WomPlatform.Connector.Models;

namespace WomPlatform.Web.Api {

    /// <summary>
    /// Temporary adapter needed for transition identifiers.
    /// </summary>
    public static class IdentifierAdapter {

        public static long ToLong(this Identifier id) {
            if(!long.TryParse(id.Id, out var numId)) {
                throw new ArgumentException("New GUID-based identifier format not supported yet");
            }
            return numId;
        }

        public static Identifier ToId(this long id) {
            return new Identifier(id.ToString());
        }

    }

}
