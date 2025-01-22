using System;
using MongoDB.Bson;

namespace WomPlatform.Web.Api.DTO {
    public class FiltersDTO {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ObjectId[] SourceIds { get; set; }
        public ObjectId[] MerchantIds { get; set; }
        public string[] AimFilter { get; set; }
    }
}
