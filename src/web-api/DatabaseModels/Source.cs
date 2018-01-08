using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("Sources")]
    public class Source {

        [Key]
        public long Id { get; set; }

        public string Key { get; set; }

        public DateTime CreationDate { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Url { get; set; }

        public long AimId { get; set; }

        public long ContactId { get; set; }

    }

}
