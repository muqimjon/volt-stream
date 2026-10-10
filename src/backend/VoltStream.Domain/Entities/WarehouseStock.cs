namespace VoltStream.Domain.Entities;

public class WarehouseStock : Auditable
{
    public int RollCount { get; set; }
    public decimal LengthPerRoll { get; set; }
    public decimal TotalLength { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public uint Version { get; set; }

    public long ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public long WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;
}
