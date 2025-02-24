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
            List<object> csvData = new List<object>();

            if (filters.StartDate.HasValue) {
                csvData.Add(new { Period = "Filter", Metric = "Start Date", Value = filters.StartDate.Value.ToString("yyyy-MM-dd"), Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = "" });
            }

            if (filters.EndDate.HasValue) {
                csvData.Add(new { Period = "Filter", Metric = "End Date", Value = filters.EndDate.Value.ToString("yyyy-MM-dd"), Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = "" });
            }

            if (filters.MerchantIds != null && filters.MerchantIds.Length > 0) {
                csvData.Add(new {
                    Period = "Filter",
                    Metric = "Merchant",
                    Value = "",
                    Merchant_ID = string.Join(", ", filters.MerchantIds.Select(id => id.ToString())),
                    Merchant_Name = string.Join(", ", filters.MerchantNames),
                    Source_ID = "",
                    Source_Name = "",
                    Aim_Code = "",
                    Aim_Name = "",
                    Rank_Type = "",
                    Rank_Position = "",
                    Rank_Name = ""
                });
            }

            if (filters.SourceIds != null && filters.SourceIds.Length > 0) {
                csvData.Add(new {
                    Period = "Filter",
                    Metric = "Source",
                    Value = "",
                    Merchant_ID = "",
                    Merchant_Name = "",
                    Source_ID = string.Join(", ", filters.SourceIds.Select(id => id.ToString())),
                    Source_Name = string.Join(", ", filters.SourceNames),
                    Aim_Code = "",
                    Aim_Name = "",
                    Rank_Type = "",
                    Rank_Position = "",
                    Rank_Name = ""
                });
            }

            if (filters.AimFilter != null && filters.AimFilter.Length > 0) {
                csvData.Add(new { Period = "Filter", Metric = "Aim Filter", Value = string.Join(", ", filters.AimFilter), Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = "" });
            }

            csvData.Add(new { Period = "Total", Metric = "Consumed", Value = consumedResponse.ConsumedInPeriod, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = "" });
            csvData.Add(new { Period = "Total", Metric = "Transactions", Value = consumedResponse.TransactionsInPeriod, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = "" });
            csvData.Add(new { Period = "Total", Metric = "Consumed Ever", Value = consumedResponse.TotalConsumed, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = "" });
            csvData.Add(new { Period = "Total", Metric = "Transactions Ever", Value = consumedResponse.TotalTransactions, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = "" });
            csvData.Add(new { Period = "Total", Metric = "Available", Value = availableResponse, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = "" });
            csvData.Add(new { Period = "Total", Metric = "Generated", Value = genRedResponse.GeneratedInPeriod, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = "" });
            csvData.Add(new { Period = "Total", Metric = "Generated Ever", Value = genRedResponse.TotalGenerated, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = "" });
            csvData.Add(new { Period = "Total", Metric = "Redeemed", Value = genRedResponse.RedeemedInPeriod, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = "" });
            csvData.Add(new { Period = "Total", Metric = "Redeemed Ever", Value = genRedResponse.TotalRedeemed, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = "" });

            csvData.AddRange(consumedResponse.TotalConsumedOverTime.Select(item => new {
                Period = item.Date, Metric = "Consumed", Value = item.Total, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = ""
            }));

            csvData.AddRange(genRedResponse.TotalGeneratedAndRedeemedOverTime.Select(item => new {
                Period = item.Date, Metric = "Generated", Value = item.TotalGenerated, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = ""
            }));

            csvData.AddRange(genRedResponse.TotalGeneratedAndRedeemedOverTime.Select(item => new {
                Period = item.Date, Metric = "Redeemed", Value = item.TotalRedeemed, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "", Rank_Position = "", Rank_Name = ""
            }));

            csvData.AddRange(consumedResponse.MerchantRanks.Select(item => new {
                Period = "", Metric = "Rank", Value = item.Amount, Merchant_ID = "", Merchant_Name = item.Name, Source_ID = "", Source_Name = "", Aim_Code = "", Aim_Name = "", Rank_Type = "Merchant Consumed", Rank_Position = item.Rank, Rank_Name = item.Name
            }));

            csvData.AddRange(genRedResponse.SourceRank.Select(item => new {
                Period = "", Metric = "Rank", Value = item.TotalGeneratedAmount, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = item.Name, Aim_Code = "", Aim_Name = "", Rank_Type = "Instrument Generated", Rank_Position = item.Rank, Rank_Name = item.Name
            }));

            csvData.AddRange(genRedResponse.SourceRank.Select(item => new {
                Period = "", Metric = "Rank", Value = item.TotalRedeemedAmount, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = item.Name, Aim_Code = "", Aim_Name = "", Rank_Type = "Instrument Redeemed", Rank_Position = item.Rank, Rank_Name = item.Name
            }));

            csvData.AddRange(genRedResponse.VoucherByAim.Select(item => new {
                Period = "", Metric = "Generated by Aim", Value = item.Amount, Merchant_ID = "", Merchant_Name = "", Source_ID = "", Source_Name = "", Aim_Code = item.AimCode, Aim_Name = item.AimName, Rank_Type = "", Rank_Position = "", Rank_Name = ""
            }));

            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(memoryStream, leaveOpen: true))
            using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture)) {
                csv.WriteRecords(csvData);
                writer.Flush();
                return memoryStream.ToArray();
            }
        }
    }
}
