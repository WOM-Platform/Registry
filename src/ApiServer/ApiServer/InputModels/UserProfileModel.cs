using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.InputModels {
    public class UserProfileModel {

        [Display(Name = "E-mail address")]
        public string Email { get; set; }

        [Required]
        [StringLength(128, MinimumLength = 3)]
        [Display(Name = "First name")]
        public string Name { get; set; }

        [Required]
        [StringLength(128, MinimumLength = 3)]
        [Display(Name = "Surname")]
        public string Surname { get; set; }

    }
}
