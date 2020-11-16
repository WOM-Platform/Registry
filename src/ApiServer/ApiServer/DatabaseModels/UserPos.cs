using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("UserPOSMap")]
    public class UserPos {

        public long UserId { get; set; }

        public long PosId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        [ForeignKey(nameof(PosId))]
        public Pos Pos { get; set; }

    }

}
