namespace VoltStream.Domain.Entities;

public class Warehouse : Auditable
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;

    public ICollection<Branch> Branches { get; set; } = default!;

    public ICollection<WarehouseStock> Stocks { get; set; } = default!;
}