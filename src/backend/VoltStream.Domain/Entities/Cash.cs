namespace VoltStream.Domain.Entities;

public class Cash : Auditable
{
    public decimal Balance { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; } = true;

    public long CurrencyId { get; set; }
    public Currency Currency { get; set; } = default!;
}
