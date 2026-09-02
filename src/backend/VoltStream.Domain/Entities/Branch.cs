namespace VoltStream.Domain.Entities;

public class Branch : Auditable
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<Warehouse> Warehouses { get; set; } = default!;
}
