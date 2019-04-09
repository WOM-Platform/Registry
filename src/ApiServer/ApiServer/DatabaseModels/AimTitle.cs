using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("AimTitles")]
    public class AimTitle {

        public string Code { get; set; }

        public string LanguageCode { get; set; }

        public string Title { get; set; }

        [ForeignKey(nameof(Code))]
        public Aim Aim { get; set; }

    }

}
