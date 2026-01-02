using System.Security.Claims;
using MongoDB.Bson;
using WomPlatform.Web.Api.Authentication;

namespace WomPlatform.Web.Api {
    public static class UserExtensions {
        private static bool GetClaimValue(this ClaimsPrincipal principal, string claimType, out ObjectId claimValue) {
            claimValue = ObjectId.Empty;

            if(principal == null) {
                return false;
            }

            var nameId = principal.FindFirstValue(claimType);
            if(nameId == null) {
                return false;
            }

            if(!ObjectId.TryParse(nameId, out ObjectId result)) {
                return false;
            }

            claimValue = result;
            return true;
        }

        /// <summary>
        /// Retrieves the user ID of the logged in user.
        /// </summary>
        /// <remarks>
        /// This will fail for API key authenticated requests, as those do not have a user ID (<see cref="ClaimTypes.NameIdentifier"/>).
        /// </remarks>
        public static bool GetUserId(this ClaimsPrincipal principal, out ObjectId userId) {
            return GetClaimValue(principal, ClaimTypes.NameIdentifier, out userId);
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

        /// <summary>
        /// Retrieves the source ID of the logged in user.
        /// </summary>
        /// <remarks>
        /// This will fail for Bearer token authenticated requests, as those do not have an unique controlled source ID.
        /// </remarks>
        public static bool GetSourceId(this ClaimsPrincipal principal, out ObjectId sourceId) {
            return GetClaimValue(principal, RegistryClaims.SourceId, out sourceId);
        }
    }
}
