using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public static class LoggingEvents {

        public const int Database = 1000;
        public const int DatabaseConnection = 1001;
        public const int DatabaseOperation = 1002;

        public const int KeyManagement = 2000;

        public const int Payment = 3000;
        public const int PaymentCreation = 3001;
        public const int PaymentProcessing = 3002;
        public const int PaymentVerification = 3003;
        public const int PaymentInformationAccess = 3004;

        public const int Voucher = 4000;
        public const int VoucherCreation = 4001;
        public const int VoucherRedemption = 4002;
        public const int VoucherVerification = 4003;

        public const int Crypto = 6000;

    }

}
