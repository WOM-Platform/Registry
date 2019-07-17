using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("ChangeLog")]
    public class ChangeLogEntry {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Code { get; set; }

        public DateTime Timestamp { get; set; }

        public string Note { get; set; }

    }

}
