using System;
using MongoDB.Bson;
using WomPlatform.Connector.Models;

namespace WomPlatform.Web.Api {

    public static class IdentifierExtension {

        public static bool Matches(this Identifier id, ObjectId objId) {
            return string.Equals(id.Id, objId.ToString(), StringComparison.InvariantCulture);
        }

        public static string GetBaseId(this Identifier id) {
            int pos = id.Id.IndexOf('/');
            if(pos >= 0) {
                return id.Id.Substring(0, pos);
            }
            else {
                return id.Id;
            }
        }

    }

}
