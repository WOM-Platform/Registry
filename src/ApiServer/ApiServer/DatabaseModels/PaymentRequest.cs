using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("PaymentRequests")]
    public class PaymentRequest {

        public const int NonceMinLength = 16;
        public const int NonceMaxLength = 32;

        public const int PasswordMinLength = 4;
        public const int PasswordMaxLength = 8;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint Id { get; set; }

        public int Amount { get; set; }

        public string JsonFilter { get; set; }

        // TODO: parsed JSON filter access with [Computed]

        public Guid OtcPay { get; set; }

        public string UrlAckPocket { get; set; }

        public string UrlAckPos { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool Performed { get; set; }

        public uint PosId { get; set; }

        public byte[] Nonce { get; set; }

        public string Password { get; set; }

    }

}
