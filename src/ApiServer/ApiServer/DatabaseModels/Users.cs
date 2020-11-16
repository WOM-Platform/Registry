using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("Users")]
    public class User {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Username { get; set; }

        public string PasswordSchema { get; set; }

        public string PasswordHash { get; set; }

        public long ContactId { get; set; }

        [ForeignKey(nameof(ContactId))]
        public Contact Contact { get; set; }

    }

}
