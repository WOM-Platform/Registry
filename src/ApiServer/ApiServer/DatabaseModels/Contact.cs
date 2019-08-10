using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("Contacts")]
    public class Contact {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Email { get; set; }

        public string FiscalCode { get; set; }

        public string Name { get; set; }

        public string Surname { get; set; }

    }

}
