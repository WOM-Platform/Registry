using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api {

    public enum MerchantActivityType {
        [Display(Name = "Agricolture")]
        Agricolture,
        [Display(Name = "Construction")]
        Construction,
        [Display(Name = "Manifacture")]
        Manifacture,
        [Display(Name = "Retail")]
        RetailCommerce,
        [Display(Name = "Transport")]
        Transport,
        [Display(Name = "Lodging")]
        Lodging,
        [Display(Name = "Food")]
        Food,
        [Display(Name = "Information")]
        Information,
        [Display(Name = "Finance")]
        Finance,
        [Display(Name = "Travel")]
        Travel,
        [Display(Name = "Education")]
        Education,
        [Display(Name = "Health")]
        Health,
        [Display(Name = "Sports")]
        Sports,
        [Display(Name = "Services")]
        Services,
        [Display(Name = "Entertainment")]
        Entertainment,
        [Display(Name = "Public organizations")]
        Organizations
    }

}
