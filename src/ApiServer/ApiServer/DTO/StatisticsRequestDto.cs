using System;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.DTO {
    public class StatisticsRequestDto {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ObjectId[] MerchantIds { get; set; }
        public ObjectId[] SourceId { get; set; }

        public string[] AimListFilter { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? Radius { get; set; }

        public override string ToString() {
            return $"StartDate: {StartDate}, EndDate: {EndDate}, MerchantId: {MerchantIds}, SourceId: {SourceId}, " +
                   $"AimListFilter: [{string.Join(", ", AimListFilter ?? new string[0])}], Latitude: {Latitude}, Longitude: {Longitude}, Radius: {Radius}";
        }
    }
}
