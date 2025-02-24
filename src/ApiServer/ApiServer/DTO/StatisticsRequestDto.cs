using System;

namespace WomPlatform.Web.Api.DTO {
    public class StatisticsRequestDto {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] MerchantIds { get; set; }
        public string[] MerchantNames { get; set; }
        public string[] SourceId { get; set; }
        public string[] SourceNames { get; set; }

        public string[] AimListFilter { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? Radius { get; set; }

        public override string ToString() {
            return $"StartDate: {StartDate}, EndDate: {EndDate}, MerchantIds: {string.Join(", ", MerchantIds ?? new string[0])}, SourceId: [{string.Join(", ", SourceId ?? new string[0])}], " +
                   $"AimListFilter: [{string.Join(", ", AimListFilter ?? new string[0])}], Latitude: {Latitude}, Longitude: {Longitude}, Radius: {Radius}";
        }
    }
}
