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

        public int Order { get; set; }

        [InverseProperty(nameof(AimTitle.Aim))]
        public ICollection<AimTitle> Titles { get; set; } = new List<AimTitle>();

    }
}
