using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.Utilities;

public class DateRangeHelper {
    // format the data based on the range length
    public static string GetDateFormatForRange(DateTime startDate, DateTime endDate)
    {
        CheckDateValidity(startDate, endDate);

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

    public static (DateTime? parsedStartDate, DateTime? parsedEndDate) ParseAndValidateDates(string startDate, string endDate)
    {
        DateTime? parsedStartDate = null;
        DateTime? parsedEndDate = null;

        if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
        {
            string format = "yyyy-MM-dd";

            // Try parsing the dates
            if (!DateTime.TryParseExact(startDate, format, null, System.Globalization.DateTimeStyles.None, out DateTime tempStartDate) ||
                !DateTime.TryParseExact(endDate, format, null, System.Globalization.DateTimeStyles.None, out DateTime tempEndDate))
            {
                throw new ServiceProblemException("Invalid date format. Please use 'yyyy-MM-dd'.", StatusCodes.Status400BadRequest);
            }

            // Check if start date is before end date
            CheckDateValidity(tempStartDate, tempEndDate);

            parsedStartDate = tempStartDate;
            parsedEndDate = tempEndDate;
        }

        return (parsedStartDate, parsedEndDate);
    }

    // check if start date is earlier or equal than endDate
    public static void CheckDateValidity(DateTime startDate, DateTime endDate) {
        if (endDate < startDate) {
            throw new ServiceProblemException("End date cannot be earlier than start date", StatusCodes.Status400BadRequest);
        }
    }

    public static Func<DateTime, DateTime> setDateIncrement(string netFormatDate) {
        // Check for the format and determine increments whether to add days, months, or years
        if(netFormatDate.Contains("dd")) {
            // Add days if the format includes days
            return date => date.AddDays(1);
        }
        else if(netFormatDate.Contains("MM")) {
            // Add months if the format includes months
            return date => date.AddMonths(1);
        }
        else {
            // Add years if the format includes only years
            return date => date.AddYears(1);
        }
    }
}
