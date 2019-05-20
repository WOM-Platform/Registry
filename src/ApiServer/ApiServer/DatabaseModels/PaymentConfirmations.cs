using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WomPlatform.Web.Api.DatabaseModels {

    [Table("PaymentConfirmations")]
    public class PaymentConfirmations {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public long PaymentRequestId { get; set; }

        [ForeignKey(nameof(PaymentRequestId))]
        public PaymentRequest Request { get; set; }

        public DateTime PerformedAt { get; set; }

    }

}
