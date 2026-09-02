namespace VoltStream.Application.Features.Sales.DTOs;

using VoltStream.Application.Features.Currencies.DTOs;
using VoltStream.Application.Features.CustomerOperations.DTOs;
using VoltStream.Application.Features.Customers.DTOs;

public record SaleDto
{
    public long Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public int RollCount { get; set; }
    public decimal Length { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDiscountApplied { get; set; }

    public long BranchId { get; set; }

    public long CustomerOperationId { get; set; }
    public CustomerOperationForSaleDto CustomerOperation { get; set; } = default!;

    public long CurrencyId { get; set; }
    public CurrencyDto Currency { get; set; } = default!;

    public long CustomerId { get; set; }
    public CustomerDto Customer { get; set; } = default!;

    public ICollection<SaleItemDto> Items { get; set; } = default!;
}
