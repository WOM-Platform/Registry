using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("GenerationRequests")]
    public class GenerationRequest {

        [Key]
        public long Id { get; set; }

        public int Amount { get; set; }

        public string Otc { get; set; }

        public DateTime CreatedAt { get; set; }

        public string State { get; set; }

        public long SourceId { get; set; }

    }

}
