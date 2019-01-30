using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("GenerationRequests")]
    public class GenerationRequest {

        public const int NonceMinLength = 16;
        public const int NonceMaxLength = 32;

        public const int PasswordMinLength = 4;
        public const int PasswordMaxLength = 8;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public int Amount { get; set; }

        public Guid OtcGen { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool Performed { get; set; }

        public long SourceId { get; set; }

        public byte[] Nonce { get; set; }

        public string Password { get; set; }

    }

}
