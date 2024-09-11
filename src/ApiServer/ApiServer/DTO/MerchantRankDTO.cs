using MongoDB.Bson;

namespace WomPlatform.Web.Api.DTO;

public class MerchantRankDTO {
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public int Amount { get; set; }
    public int Rank { get; set; }
}
