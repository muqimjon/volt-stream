namespace ApiServices.Models.Responses;

public record SupplyResponse
{
    public long Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public long? BranchId { get; set; }
    public int RollCount { get; set; }
    public decimal LengthPerRoll { get; set; }
    public decimal TotalLength { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }

    public long ProductId { get; set; }
    public ProductResponse Product { get; set; } = default!;
}