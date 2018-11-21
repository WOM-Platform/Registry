using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("Aims")]
    public class Aim {

        [Key]
        public uint Id { get; set; }

        public string Description { get; set; }

        public uint ContactId { get; set; }

        public DateTime CreationDate { get; set; }

    }
}
