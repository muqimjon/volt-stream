namespace VoltStream.Domain.Entities;

public class Warehouse : Auditable
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;

    public long BranchId { get; set; }
    public Branch Branch { get; set; } = default!;

    public ICollection<WarehouseStock> Stocks { get; set; } = default!;
}