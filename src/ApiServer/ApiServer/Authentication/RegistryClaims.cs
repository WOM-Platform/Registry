namespace WomPlatform.Web.Api.Authentication {
    public static class RegistryClaims {
        /// <summary>
        /// Claim that specifies the role of the user in the WOM platform.
        /// The role is expressed as enum values of <see cref="PlatformRole"/>.
        /// </summary>
        public const string Role = "wom_role";

        /// <summary>
        /// Claim that specifies the source ID the user is operating as an administrator of.
        /// If the claim is set, the user is considered to be an administrator of the source with the given ID.
        /// </summary>
        public const string SourceId = "wom_source_id";
    }
}
