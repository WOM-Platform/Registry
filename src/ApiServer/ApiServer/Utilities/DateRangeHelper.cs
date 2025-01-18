using System;
using Microsoft.AspNetCore.Http;

namespace WomPlatform.Web.Api.Utilities;

public class DateRangeHelper {
    // format the data based on the range length
    public static string GetDateFormatForRange(DateTime startDate, DateTime endDate, bool? isDailyGranularity) {
        CheckDateValidity(startDate, endDate);

        // check if format date is not daily set for CSV
        if(isDailyGranularity.HasValue && isDailyGranularity == false) {
            var totalDays = (endDate - startDate).TotalDays;

            // save current day to check number of days in the current month
            var today = DateTime.Now;
            var lastMonth = today.AddMonths(-1);

            if(totalDays <= (today - lastMonth).TotalDays) {
                return "%Y-%m-%d"; // Group by day
            }

            if(totalDays <= (DateTime.Today - DateTime.Today.AddYears(-1)).TotalDays) {
                return "%Y-%m"; // Group by month
            }

            return "%Y"; // Group by year
        }

        return "%Y-%m-%d"; // Group by day
    }

    public static (DateTime? parsedStartDate, DateTime? parsedEndDate) ParseAndValidateDates(DateTime? startDate, DateTime? endDate) {
        DateTime? parsedStartDate = null;
        DateTime? parsedEndDate = null;

        // Check if startDate and endDate are provided
        if(startDate.HasValue && endDate.HasValue) {
            // Ensure startDate is before or equal to endDate
            CheckDateValidity(startDate.Value, endDate.Value);

            // Assign values since they are valid
            parsedStartDate = startDate;
            parsedEndDate = endDate;
        }

        return (parsedStartDate, parsedEndDate);
    }


    // check if start date is earlier or equal than endDate
    public static void CheckDateValidity(DateTime startDate, DateTime endDate) {
        if(endDate < startDate) {
            throw new ServiceProblemException("End date cannot be earlier than start date", StatusCodes.Status400BadRequest);
        }
    }

    public static Func<DateTime, DateTime> SetDateIncrement(string netFormatDate) {
        // Check for the format and determine increments whether to add days, months, or years
        if(netFormatDate.Contains("dd")) {
            // Add days if the format includes days
            return date => date.AddDays(1);
        }

        if(netFormatDate.Contains("MM")) {
            // Add months if the format includes months
            return date => date.AddMonths(1);
        }

        // Add years if the format includes only years
        return date => date.AddYears(1);
    }
}
