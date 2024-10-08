using System;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.DTO;

public class StatisticsRequestDto {
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public ObjectId? MerchantId { get; set; }
    public ObjectId? SourceId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Radius { get; set; }
}
