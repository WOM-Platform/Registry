using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.InputModels {

    public class UserRegisterMerchantModel {

        [Required]
        [EmailAddress]
        [Display(Name = "Indirizzo e-mail")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [StringLength(128, MinimumLength = 3)]
        [Display(Name = "Nome")]
        public string Name { get; set; }

        [Required]
        [StringLength(128, MinimumLength = 3)]
        [Display(Name = "Cognome")]
        public string Surname { get; set; }

        [Required]
        [StringLength(256, MinimumLength = 5)]
        [Display(Name = "Ragione sociale del Merchant")]
        public string MerchantTitle { get; set; }

        [Required]
        [Display(Name = "Codice fiscale")]
        [StringLength(16, MinimumLength = 16)]
        public string MerchantFiscalCode { get; set; }

        [Display(Name = "Indirizzo")]
        public string MerchantAddress { get; set; }

        [Display(Name = "CAP")]
        public string MerchantZipCode { get; set; }

        [Display(Name = "Città")]
        public string MerchantCity { get; set; }

        [Display(Name = "Paese")]
        public string MerchantNation { get; set; }

        [Required]
        [Display(Name = "Attività primaria")]
        public MerchantActivityType MerchantActivityType { get; set; }

        [Display(Name = "Sito Web")]
        [Url]
        public string MerchantWebsite { get; set; }

        [Required]
        [Display(Name = "Descrizione")]
        [StringLength(8000, MinimumLength = 10)]
        [DataType(DataType.MultilineText)]
        public string MerchantDescription { get; set; }

        [Required]
        [Checked]
        [Display(Name = "Accetta termini e condizioni")]
        public bool Accept { get; set; }

    }

}
