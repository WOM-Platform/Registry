using System;
using System.Security.Claims;
using MongoDB.Bson;

namespace WomPlatform.Web.Api {

    public static class UserExtensions {

        /// <summary>
        /// Retrives the user ID of the logged in user.
        /// </summary>
        public static bool GetUserId(this ClaimsPrincipal principal, out ObjectId userId) {
            userId = ObjectId.Empty;

            if(principal == null) {
                return false;
            }

            var nameId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if(nameId == null) {
                return false;
            }

            if(!ObjectId.TryParse(nameId, out ObjectId result)) {
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

        /// <summary>
        /// Extracts the current session ID from the authentication claims.
        /// </summary>
        public static bool GetSessionId(this ClaimsPrincipal principal, out Guid sessionId) {
            sessionId = Guid.Empty;

            if(principal == null) {
                return false;
            }

            var sessionValue = principal.FindFirstValue(Startup.CookieSessionClaimType);
            if(sessionValue == null) {
                return false;
            }

            if(!Guid.TryParseExact(sessionValue, "N", out Guid result)) {
                return false;
            }

            sessionId = result;
            return true;
        }

    }

}
