using System;
using System.Collections.Generic;
using System.Text;

namespace ApiTester {

    public static class DateTimeExtensions {

        public static DateTime ToSecondPrecision(this DateTime dt) {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute,
                dt.Second + ((dt.Millisecond > 499) ? 1 : 0),
                DateTimeKind.Utc);
        }

    }

}
