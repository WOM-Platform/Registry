using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using WomPlatform.Web.Api.DTO;

namespace WomPlatform.Web.Api.Utilities;

public class CsvFileHelper {
    public static byte[] GenerateCsvContent(VoucherGenerationRedemptionStatsResponse genRedResponse, VoucherConsumptionStatsResponse consumedResponse)
    {
        var csvData = new List<object>
        {
            new { Category = "Totale WOM Consumed", Value = consumedResponse.TotalConsumed },
            new { Category = "Totale WOM Generated", Value = genRedResponse.TotalGenerated },
            new { Category = "Totale WOM Redeemed", Value = genRedResponse.TotalRedeemed }
        };

        csvData.AddRange(consumedResponse.TotalConsumedOverTime.Select(item => new { Category = $"Consumed Over Time ({item.Date})", Value = item.Total }));
        csvData.AddRange(genRedResponse.TotalGeneratedAndRedeemedOverTime.Select(item => new { Category = $"Generated ({item.Date})", Value = item.TotalGenerated }));
        csvData.AddRange(genRedResponse.TotalGeneratedAndRedeemedOverTime.Select(item => new { Category = $"Redeemed ({item.Date})", Value = item.TotalRedeemed }));
        csvData.AddRange(consumedResponse.MerchantRanks.Select(item => new { Category = $"Merchant Rank {item.Rank} ({item.Name})", Value = item.Amount }));
        csvData.AddRange(consumedResponse.VoucherByAims.Select(item => new { Category = $"Voucher Consumed ({item.AimCode})", Value = item.Amount }));
        csvData.AddRange(genRedResponse.VoucherByAim.Select(item => new { Category = $"Voucher Generated ({item.AimCode})", Value = item.Amount }));

        using (var memoryStream = new MemoryStream())
        using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(csvData);  // Write the records to the CSV file
            writer.Flush(); // Ensure all data is written to the memory stream
            return memoryStream.ToArray(); // Return the CSV data as a byte array
        }
    }
}
