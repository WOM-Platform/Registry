using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("POS")]
    public class Pos {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }

        public string PublicKey { get; set; }

        public string PrivateKey { get; set; }

        public DateTime CreationDate { get; set; }

        public string Url { get; set; }

    }

}
