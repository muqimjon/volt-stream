namespace VoltStream.Domain.Entities;

public class SaleDraftItem : Auditable
{
    public int RollCount { get; set; }
    public decimal LengthPerRoll { get; set; }
    public decimal TotalLength { get; set; }

    public long SaleDraftId { get; set; }
    public SaleDraft SaleDraft { get; set; } = default!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = default!;
}
