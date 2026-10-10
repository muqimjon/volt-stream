namespace VoltStream.Domain.Entities;

using VoltStream.Domain.Enums;

public class SaleDraft : Auditable
{
    public DateTimeOffset Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public SaleDraftStatus Status { get; set; }

    public long BranchId { get; set; }
    public Branch Branch { get; set; } = default!;

    public long? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public long? ConfirmedSaleId { get; set; }
    public Sale? ConfirmedSale { get; set; }

    public ICollection<SaleDraftItem> Items { get; set; } = [];
}
