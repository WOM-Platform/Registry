using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public static class LoggingEvents {

        public const int Database = 1000;

        public const int KeyManagement = 2000;

        public const int Payment = 3000;
        public const int PaymentCreation = 3001;
        public const int PaymentProcessing = 3002;

        public const int Voucher = 4000;
        public const int VoucherCreation = 4001;
        public const int VoucherRedemption = 4002;

    }

}
