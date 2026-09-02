namespace VoltStream.Application.Features.Sales.DTOs;

using VoltStream.Application.Features.Products.DTOs;

public record SaleItemDto
{
    public long Id { get; set; }
    public int RollCount { get; set; }
    public decimal LengthPerRoll { get; set; }
    public decimal TotalLength { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal Weight { get; set; }

    public long ProductId { get; set; }
    public ProductDto Product { get; set; } = default!;

    public long SaleId { get; set; }
}