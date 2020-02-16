using System.ComponentModel.DataAnnotations;

namespace WomPlatform.Web.Api {

    public enum MerchantActivityType {
        [Display(Name = "Agricolture")]
        Agricolture,
        [Display(Name = "Construction")]
        Construction,
        [Display(Name = "Manifacture")]
        Manifacture,
        [Display(Name = "Retail commerce")]
        RetailCommerce,
        [Display(Name = "Transport")]
        Transport,
        [Display(Name = "Lodging and accomodation")]
        Lodging,
        [Display(Name = "Food and catering")]
        Food,
        [Display(Name = "Information")]
        Information,
        [Display(Name = "Financial services")]
        Finance,
        [Display(Name = "Travel")]
        Travel,
        [Display(Name = "Education")]
        Education,
        [Display(Name = "Health and wellbeing")]
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
