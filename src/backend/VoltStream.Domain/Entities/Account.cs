namespace VoltStream.Domain.Entities;

public class Account : Auditable
{
    public decimal OpeningBalance { get; set; }
    public decimal Discount { get; set; }

    public long CurrencyId { get; set; }
    public Currency Currency { get; set; } = default!;

    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
}
