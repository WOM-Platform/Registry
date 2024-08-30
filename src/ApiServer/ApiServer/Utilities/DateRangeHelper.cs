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

    // check if start date is earlier or equal than endDate
    public static void CheckDateValidity(DateTime startDate, DateTime endDate) {
        if (endDate < startDate) {
            throw new ServiceProblemException("End date cannot be earlier than start date", StatusCodes.Status400BadRequest);
        }
    }
}
