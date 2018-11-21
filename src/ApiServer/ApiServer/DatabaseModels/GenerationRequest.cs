using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("GenerationRequests")]
    public class GenerationRequest {

        [Key]
        public uint Id { get; set; }

        public ushort Amount { get; set; }

        public string OtcGen { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool Performed { get; set; }

        public uint SourceId { get; set; }

    }

}
