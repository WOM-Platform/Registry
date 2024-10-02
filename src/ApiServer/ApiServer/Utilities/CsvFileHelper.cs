using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using WomPlatform.Web.Api.DTO;

namespace WomPlatform.Web.Api.Utilities;

public class CsvFileHelper {
    public static byte[] GenerateCsvContent(StatisticsRequestDto statisticsRequestDto)
    {
        var totalConsumedOverTime = new List<TotalConsumedOverTimeDto>
        {
            new TotalConsumedOverTimeDto { Total = 20, Date = "datyadoiasoi" }
        };

        using (var memoryStream = new MemoryStream())
        using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(totalConsumedOverTime);  // Write the records to the CSV file
            writer.Flush(); // Ensure all data is written to the memory stream
            return memoryStream.ToArray(); // Return the CSV data as a byte array
        }
    }
}
