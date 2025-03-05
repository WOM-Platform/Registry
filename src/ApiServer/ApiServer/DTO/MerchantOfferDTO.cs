using MongoDB.Bson;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.DTO;

public class MerchantOfferDTO {
    public ObjectId Id { get; set; }
    public string Title { get; set; }
    public Merchant Merchant {get; set; }
    public Pos Pos { get; set; }
    public Filter Filter { get; set; }
    public string Description { get; set; }
    public int Cost { get; set; }
    public int TotalAmount { get; set; }
    public int TransactionsNumber { get; set; }

}
