namespace VoltStream.Application.Features.Supplies.DTOs;

using VoltStream.Application.Features.Categories.DTOs;
using VoltStream.Application.Features.Products.DTOs;

public record SupplyDto
{
    public long Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public int RollCount { get; set; }
    public decimal LengthPerRoll { get; set; }
    public decimal TotalLength { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }

    public long BranchId { get; set; }

    public long ProductId { get; set; }
    public ProductDto Product { get; set; } = default!;

    public long CategoryId { get; set; }
    public CategoryDto Category { get; set; } = default!;
}
