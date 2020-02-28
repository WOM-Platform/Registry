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
        [StringLength(256, MinimumLength = 5)]
        [Display(Name = "Merchant Name")]
        public string MerchantTitle { get; set; }

        [Required]
        [Display(Name = "Fiscal code")]
        [StringLength(16, MinimumLength = 16)]
        public string MerchantFiscalCode { get; set; }

        [Display(Name = "Address")]
        public string MerchantAddress { get; set; }

        [Display(Name = "ZIP")]
        public string MerchantZipCode { get; set; }

        [Display(Name = "City")]
        public string MerchantCity { get; set; }

        [Display(Name = "Nation")]
        public string MerchantNation { get; set; }

        [Required]
        [Display(Name = "Primary activity")]
        public MerchantActivityType MerchantActivityType { get; set; }

        [Display(Name = "Web site")]
        [Url]
        public string MerchantWebsite { get; set; }

        [Required]
        [Display(Name = "Description", Description = "Description of the company, social aim, and how WOMs would be monetized.")]
        [StringLength(8000, MinimumLength = 100)]
        [DataType(DataType.MultilineText)]
        public string MerchantDescription { get; set; }

        [Required]
        [Checked]
        [Display(Name = "Accept terms and conditions")]
        public bool Accept { get; set; }

    }

}
