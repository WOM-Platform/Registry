namespace WomPlatform.Web.Api.DTO;

public class TotalConsumedOverTimeDTO {
    public string Date { get; set; }
    public int Total { get; set; }
}


public class TotalGeneratedAndRedeemedOverTimeDTO {
    public string Date { get; set; }
    public int TotalGenerated { get; set; }
    public int TotalRedeemed { get; set; }

}
