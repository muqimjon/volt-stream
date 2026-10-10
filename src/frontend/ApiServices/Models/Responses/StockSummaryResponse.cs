namespace ApiServices.Models.Responses;

public record StockSummaryResponse
{
    public long ProductId { get; set; }
    public int RollCount { get; set; }
    public decimal TotalLength { get; set; }
}
