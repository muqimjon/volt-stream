namespace ApiServices.Models.Requests;

public record SaleItemHistoryRequest
{
    public DateTimeOffset BeginDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public long? CustomerId { get; set; }
    public long? CategoryId { get; set; }
    public long? ProductId { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
