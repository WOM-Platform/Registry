using System;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace WomPlatform.Web.Api.Utilities;

public class DateRangeHelper {
    public static string GetDateFormatForRange(DateTime startDate, DateTime endDate)
    {
        var totalDays = (endDate - startDate).TotalDays;

        if (totalDays <= 7)
        {
            return "%Y-%m-%d"; // Group by day
        }
        else if (totalDays <= 365)
        {
            return "%Y-%m"; // Group by month
        }
        else
        {
            return "%Y"; // Group by year
        }
    }

    public static IActionResult CheckDateValidity(DateTime startDate, DateTime endDate) {
        if (endDate < startDate) {
            return new BadRequestObjectResult("End date cannot be earlier than start date.");
        }
        return new OkObjectResult("Date is valid");
    }
}
