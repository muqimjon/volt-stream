namespace VoltStream.Application.Features.Products.DTOs;

using VoltStream.Application.Features.Categories.DTOs;

public record ProductForWarehouseDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsWeightTracked { get; set; }
    public decimal? WeightPerUnit { get; set; }

    public long CategoryId { get; set; }
    public CategoryForProductDto? Category { get; set; } = default!;
}
