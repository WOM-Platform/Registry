using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using WomPlatform.Web.Api.DTO;

namespace WomPlatform.Web.Api.Utilities {
    public class CsvFileHelper {
        public static byte[] GenerateCsvContent(VoucherGenerationRedemptionStatsResponse genRedResponse,
            VoucherConsumptionStatsResponse consumedResponse, int availableResponse, FiltersDTO filters) {
            // Start by adding metadata for the filters at the top of the CSV
            List<object> csvData = new List<object>();

            // Add filters information if present
            if(filters.StartDate.HasValue) {
                csvData.Add(new { Period = "Filter", Metric = "Start Date", Value = filters.StartDate.Value.ToString("yyyy-MM-dd"), Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = "" });
            }

            if(filters.EndDate.HasValue) {
                csvData.Add(new { Period = "Filter", Metric = "End Date", Value = filters.EndDate.Value.ToString("yyyy-MM-dd"), Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = "" });
            }

            if(filters.SourceIds != null && filters.SourceIds.Length > 0) {
                csvData.Add(new { Period = "Filter", Metric = "Source ID", Value = filters.SourceIds.ToString(), Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = "" });
            }

            if(filters.MerchantIds != null && filters.MerchantIds.Length > 0) {
                csvData.Add(new { Period = "Filter", Metric = "Merchant ID", Value = filters.MerchantIds.ToString(), Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = "" });
            }

            if(filters.AimFilter != null && filters.AimFilter.Length > 0) {
                csvData.Add(new { Period = "Filter", Metric = "Aim Filter", Value = string.Join(", ", filters.AimFilter), Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = "" });
            }

            // Add the main data rows
            csvData.Add(new { Period = "Total", Metric = "Consumed", Value = consumedResponse.TotalConsumed, Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = "" });
            csvData.Add(new { Period = "Total", Metric = "Consumed Ever", Value = consumedResponse.TotalEverConsumed, Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = "" });
            csvData.Add(new { Period = "Total", Metric = "Available", Value = availableResponse, Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = "" });
            csvData.Add(new { Period = "Total", Metric = "Generated", Value = genRedResponse.TotalGenerated, Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = "" });
            csvData.Add(new { Period = "Total", Metric = "Generated Ever", Value = genRedResponse.TotalEverGenerated, Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = "" });
            csvData.Add(new { Period = "Total", Metric = "Redeemed", Value = genRedResponse.TotalRedeemed, Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = "" });
            csvData.Add(new { Period = "Total", Metric = "Redeemed Ever", Value = genRedResponse.TotalEverRedeemed, Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = "" });

            csvData.AddRange(consumedResponse.TotalConsumedOverTime.Select(item => new {
                Period = item.Date, Metric = "Consumed", Value = item.Total, Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = ""
            }));

            csvData.AddRange(genRedResponse.TotalGeneratedAndRedeemedOverTime.Select(item => new {
                Period = item.Date, Metric = "Generated", Value = item.TotalGenerated, Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = ""
            }));

            csvData.AddRange(genRedResponse.TotalGeneratedAndRedeemedOverTime.Select(item => new {
                Period = item.Date, Metric = "Redeemed", Value = item.TotalRedeemed, Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = ""
            }));

            csvData.AddRange(consumedResponse.MerchantRanks.Select(item => new {
                Period = "", Metric = "Rank", Value = item.Amount, Rank_Type = "Merchant", Rank_Position = item.Rank, Rank_Name = item.Name, Aim = ""
            }));

            csvData.AddRange(genRedResponse.SourceRank.Select(item => new {
                Period = "", Metric = "Rank", Value = item.TotalGeneratedAmount, Rank_Type = "Instrument Generated", Rank_Position = item.Rank, Rank_Name = item.Name, Aim = ""
            }));

            csvData.AddRange(genRedResponse.SourceRank.Select(item => new {
                Period = "", Metric = "Rank", Value = item.TotalRedeemedAmount, Rank_Type = "Instrument Redeemed", Rank_Position = item.Rank, Rank_Name = item.Name, Aim = ""
            }));

            csvData.AddRange(consumedResponse.VoucherByAims.Select(item => new {
                Period = "", Metric = "Consumed by Aim", Value = item.Amount, Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = item.AimCode
            }));

            csvData.AddRange(genRedResponse.VoucherByAim.Select(item => new {
                Period = "", Metric = "Generated by Aim", Value = item.Amount, Rank_Type = "", Rank_Position = "", Rank_Name = "", Aim = item.AimCode
            }));

            // Write the records to CSV
            using(MemoryStream memoryStream = new MemoryStream())
            using(StreamWriter writer = new StreamWriter(memoryStream, leaveOpen: true))
            using(CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture)) {
                csv.WriteRecords(csvData); // Write the records to the CSV file
                writer.Flush(); // Ensure all data is written to the memory stream
                return memoryStream.ToArray(); // Return the CSV data as a byte array
            }
        }
    }
}
