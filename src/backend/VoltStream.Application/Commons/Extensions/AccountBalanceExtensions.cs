namespace VoltStream.Application.Commons.Extensions;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Accounts.DTOs;
using VoltStream.Application.Features.CustomerOperations.DTOs;
using VoltStream.Application.Features.Customers.DTOs;

public static class AccountBalanceExtensions
{
    public static async Task FillBalancesAsync(this IAppDbContext context, IEnumerable<AccountDto?> accounts, CancellationToken cancellationToken)
    {
        var list = accounts.OfType<AccountDto>().ToList();
        if (list.Count == 0)
            return;

        var ids = list.Select(a => a.Id).Distinct().ToList();
        var balances = await context.Accounts
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .Select(a => new
            {
                a.Id,
                Balance = a.OpeningBalance + context.CustomerOperations.Where(o => o.AccountId == a.Id).Sum(o => o.Amount)
            })
            .ToDictionaryAsync(x => x.Id, x => x.Balance, cancellationToken);

        foreach (var account in list)
            account.Balance = balances.GetValueOrDefault(account.Id);
    }

    public static IEnumerable<AccountDto?> Accounts(this IEnumerable<CustomerDto> customers)
        => customers.SelectMany(c => c.Accounts ?? []);

    public static IEnumerable<AccountDto?> Accounts(this IEnumerable<CustomerOperationDto> operations)
        => operations.SelectMany(o => (IEnumerable<AccountDto?>)[
            o.Account,
            .. o.Sale?.Customer?.Accounts ?? [],
            .. o.Payment?.Customer?.Accounts ?? []]);
}
