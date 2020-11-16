using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("UserSourceMap")]
    public class UserSource {

        public long UserId { get; set; }

        public long SourceId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        [ForeignKey(nameof(SourceId))]
        public Source Source { get; set; }

    }

}
