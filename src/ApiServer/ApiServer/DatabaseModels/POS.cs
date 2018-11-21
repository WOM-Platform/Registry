using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("POS")]
    public class POS {

        [Key]
        public uint Id { get; set; }

        public string Name { get; set; }

        public string PublicKey { get; set; }

        public DateTime CreationDate { get; set; }

        public string Url { get; set; }

    }

}
