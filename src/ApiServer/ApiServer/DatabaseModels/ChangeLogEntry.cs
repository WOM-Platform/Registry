using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("ChangeLog")]
    public class ChangeLogEntry {

        public string Id { get; set; }

        public DateTime Timestamp { get; set; }

        public string Note { get; set; }

    }

}
