using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("Sources")]
    public class Source {

        [Key]
        public uint Id { get; set; }

        public string PublicKey { get; set; }

        public DateTime CreationDate { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public uint AimId { get; set; }

        public uint ContactId { get; set; }

    }

}
