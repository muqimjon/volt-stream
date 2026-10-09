namespace VoltStream.Application.Features.Accounts.DTOs;

using VoltStream.Application.Features.Currencies.DTOs;

public record AccountDto(
    long Id,
    decimal OpeningBalance,
    decimal Discount,
    long CustomerId,
    long CurrencyId,
    CurrencyDto Currency)
{
    public decimal Balance { get; set; }
}
