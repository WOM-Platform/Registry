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

        [Column(TypeName = "CHAR(36)")]
        public Guid OtcGen { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool Verified { get; set; }

        public DateTime? PerformedAt { get; set; }

        public bool Void { get; set; }

        public long SourceId { get; set; }

        [ForeignKey(nameof(SourceId))]
        public Source Source { get; set; }

        public byte[] Nonce { get; set; }

        public string Password { get; set; }

    }

}
