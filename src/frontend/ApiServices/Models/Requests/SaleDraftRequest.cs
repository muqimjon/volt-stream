namespace ApiServices.Models.Requests;

public record SaleDraftRequest
{
    public long Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public long? BranchId { get; set; }
    public long? CustomerId { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<SaleDraftItemRequest> Items { get; set; } = [];
}

public record SaleDraftItemRequest
{
    public long ProductId { get; set; }
    public decimal LengthPerRoll { get; set; }
    public int RollCount { get; set; }
    public decimal TotalLength { get; set; }
}
