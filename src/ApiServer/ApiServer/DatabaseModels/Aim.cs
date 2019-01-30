using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("Aims")]
    public class Aim {

        [Key]
        public string Code { get; set; }

        public string Description { get; set; }

        public DateTime CreationDate { get; set; }

    }
}
