namespace WomPlatform.Web.Api.DTO;

public class TotalConsumedOverTimeDto {
    public string Date { get; set; }
    public int Total { get; set; }
}


public class TotalGeneratedAndRedeemedOverTimeDto {
    public string Date { get; set; }
    public int TotalGenerated { get; set; }
    public int TotalRedeemed { get; set; }

}
