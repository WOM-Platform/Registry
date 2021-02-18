using System.Security.Claims;
using MongoDB.Bson;

namespace WomPlatform.Web.Api {

    public static class UserExtensions {

        /// <summary>
        /// Retrives the user ID of the logged in user.
        /// </summary>
        public static bool GetUserId(this ClaimsPrincipal principal, out ObjectId userId) {
            var nameId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if(nameId == null) {
                userId = ObjectId.Empty;
                return false;
            }

            if(!ObjectId.TryParse(nameId, out ObjectId result)) {
                userId = ObjectId.Empty;
                return false;
            }

            userId = result;
            return true;
        }

        /// <summary>
        /// Verifies that the logged in user has a given user ID.
        /// </summary>
        public static bool UserIdEquals(this ClaimsPrincipal principal, ObjectId refUserId) {
            if(!principal.GetUserId(out var valUserId)) {
                return false;
            }

            return valUserId.Equals(refUserId);
        }

    }

}
