using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("POS")]
    public class POS {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint Id { get; set; }

        public string Name { get; set; }

        public string PublicKey { get; set; }

        public DateTime CreationDate { get; set; }

        public string Url { get; set; }

    }

}
