namespace ApiServices.Models.Requests;

public record SupplyRequest
{
    public long Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public long? BranchId { get; set; }
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int RollCount { get; set; }
    public decimal LengthPerRoll { get; set; }
    public decimal TotalLength { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
}