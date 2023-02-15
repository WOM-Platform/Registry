namespace WomPlatform.Web.Api {
    public enum MerchantRole {
        None    =   0, // User has no access
        User    = 128, // Can use merchant's POS
        Admin   = 255, // Can manage merchant and POS
    }
}
