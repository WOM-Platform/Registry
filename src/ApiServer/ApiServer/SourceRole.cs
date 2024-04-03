namespace WomPlatform.Web.Api {
    public enum SourceRole : byte {
        None = 0, // User has no access
        User = 128, // User has read-only access to source and can emit WOMs
        Admin = 255, // User can manage source
    }
}
