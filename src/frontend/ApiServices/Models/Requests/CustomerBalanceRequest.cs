namespace ApiServices.Models.Requests;

public record CustomerBalanceRequest
{
    public long? CustomerId { get; set; }
    public string? Sign { get; set; }
    public decimal Amount { get; set; }
    public string? Type { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
