namespace ApiServices.Models.Responses;

public record CustomerBalanceSummaryResponse
{
    public decimal Discount { get; set; }
    public decimal Debitor { get; set; }
    public decimal Creditor { get; set; }
}
