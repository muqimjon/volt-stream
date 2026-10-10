namespace VoltStream.Domain.Entities;

public class SaleItem : Auditable
{
    public int RollCount { get; set; }
    public decimal LengthPerRoll { get; set; }
    public decimal TotalLength { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal Weight { get; set; }

    public long SaleId { get; set; }
    public Sale Sale { get; set; } = default!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = default!;
}