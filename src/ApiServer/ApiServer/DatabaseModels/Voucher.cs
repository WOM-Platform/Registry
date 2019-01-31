using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("Vouchers")]
    public class Voucher {

        public const int SecretLength = 16;

        public Voucher() {
            Secret = new byte[SecretLength];
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public byte[] Secret { get; set; }

        public string AimCode { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public DateTime Timestamp { get; set; }

        public long GenerationRequestId { get; set; }

        public long? PaymentRequestId { get; set; }

        public bool Spent { get; set; }

    }

}
