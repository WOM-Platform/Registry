using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.InputModels {

    public class UserRegisterMerchantModel {

        [Required]
        [EmailAddress]
        [Display(Name = "E-mail address")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [StringLength(128, MinimumLength = 3)]
        [Display(Name = "First name")]
        public string Name { get; set; }

        [Required]
        [StringLength(128, MinimumLength = 3)]
        [Display(Name = "Surname")]
        public string Surname { get; set; }

        [Required]
        [StringLength(256, MinimumLength = 4)]
        [Display(Name = "Merchant name")]
        public string MerchantTitle { get; set; }

        [Required]
        [Display(Name = "Italian fiscal code")]
        [StringLength(16, MinimumLength = 16)]
        public string MerchantFiscalCode { get; set; }

        [Display(Name = "Address")]
        public string MerchantAddress { get; set; }

        [Display(Name = "CAP")]
        public string MerchantZipCode { get; set; }

        [Display(Name = "City")]
        public string MerchantCity { get; set; }

        [Display(Name = "Country")]
        public string MerchantNation { get; set; }

        [Required]
        [Display(Name = "Primary business")]
        public MerchantActivityType MerchantActivityType { get; set; }

        [Display(Name = "Web site")]
        [Url]
        public string MerchantWebsite { get; set; }

        [Required]
        [Display(Name = "Description")]
        [StringLength(8000, MinimumLength = 10)]
        [DataType(DataType.MultilineText)]
        public string MerchantDescription { get; set; }

        [Required(ErrorMessage = "You must accept in order to sign up")]
        [Checked]
        [Display(Name = "Accept terms and conditions")]
        public bool Accept { get; set; }

    }

}
