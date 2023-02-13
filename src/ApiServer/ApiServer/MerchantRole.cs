namespace WomPlatform.Web.Api {
    public enum MerchantRole {
        None,   // User has no access
        User,   // Can use merchant's POS
        Admin,  // Can manage merchant and POS
    }
}
