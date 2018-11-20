using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("POS")]
    public class POS {

        [Key]
        public long Id { get; set; }

        public string Name { get; set; }

        public string Key { get; set; }

        public string Description { get; set; }

        public DateTime CreationDate { get; set; }

        public string Url { get; set; }

    }

}
