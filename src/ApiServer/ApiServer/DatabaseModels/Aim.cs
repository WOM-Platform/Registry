using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("Aims")]
    public class Aim {

        [Key]
        public string Code { get; set; }

        public string IconFile { get; set; }

        [InverseProperty(nameof(AimTitle.Aim))]
        public List<AimTitle> Titles { get; set; }

    }
}
