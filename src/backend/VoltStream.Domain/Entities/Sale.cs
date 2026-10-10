namespace VoltStream.Domain.Entities;

public class Sale : Auditable
{
    public DateTimeOffset Date { get; set; }
    public int RollCount { get; set; }
    public decimal Length { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDiscountApplied { get; set; }

    public long BranchId { get; set; }
    public Branch Branch { get; set; } = default!;
    public long? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public long CurrencyId { get; set; }
    public Currency Currency { get; set; } = default!;

    public long? CustomerId { get; set; }
    public Customer? Customer { get; set; } = default!;

    public long CustomerOperationId { get; set; }
    public CustomerOperation? CustomerOperation { get; set; } = default!;

    public ICollection<SaleItem> Items { get; set; } = default!;
}