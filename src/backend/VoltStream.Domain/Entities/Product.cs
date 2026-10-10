namespace VoltStream.Domain.Entities;

public class Product : Auditable
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;
    public bool IsWeightTracked { get; set; }
    public decimal? WeightPerUnit { get; set; }

    public long CategoryId { get; set; }
    public Category Category { get; set; } = default!;

    public ICollection<WarehouseStock> Stocks { get; set; } = default!;
}