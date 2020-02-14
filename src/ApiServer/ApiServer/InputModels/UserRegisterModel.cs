using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.InputModels {

    public class UserRegisterModel {

        [Required]
        [StringLength(128, MinimumLength = 3)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "E-mail address")]
        public string Email { get; set; }

        [Display(Name = "Fiscal code")]
        [Required]
        [StringLength(16, MinimumLength = 16)]
        public string FiscalCode { get; set; }

        [Required]
        [StringLength(128, MinimumLength = 3)]
        [Display(Name = "First name")]
        public string Name { get; set; }

        [Required]
        [StringLength(128, MinimumLength = 3)]
        [Display(Name = "Surname")]
        public string Surname { get; set; }

        [Required]
        [Checked]
        [Display(Name = "Accept terms and conditions")]
        
        public bool Accept { get; set; }

    }

}
