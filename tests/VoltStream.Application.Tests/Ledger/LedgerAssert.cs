namespace VoltStream.Application.Tests.Ledger;

using VoltStream.Application.Features.Customers.Queries;
using VoltStream.Application.Tests.Infrastructure;

public static class LedgerAssert
{
    // Qoldiq uch joyda ko'rinadi: Savdo sahifasi (mijozlar so'rovi), Mijoz hisoboti va Kontragentlar. Hammasi bir xil bo'lishi shart
    public static async Task<Dictionary<long, decimal>> Consistent(World world, long customerId)
    {
        var filtered = (await world.Admin.Send(new CustomerFilterQuery
        {
            Filters = new() { ["id"] = [customerId.ToString()], ["accounts"] = ["include:currency"] }
        })).Single();
        var listed = (await world.Admin.Send(new GetAllCustomersQuery())).Single(customer => customer.Id == customerId);
        var byId = await world.Admin.Send(new GetCustomerByIdQuery(customerId));

        var page = filtered.Accounts.ToDictionary(account => account.CurrencyId, account => account.Balance);
        Same("mijozlar ro'yxati", page, listed.Accounts.ToDictionary(account => account.CurrencyId, account => account.Balance));
        Same("mijoz id bo'yicha", page, byId.Accounts.ToDictionary(account => account.CurrencyId, account => account.Balance));

        var report = await world.Report(customerId);
        Same("hisobot (hisoblar kesimida)", page, filtered.Accounts.ToDictionary(
            account => account.CurrencyId,
            account => account.OpeningBalance + report.Operations.Where(operation => operation.AccountId == account.Id).Sum(operation => operation.Amount)));

        var total = page.Values.Sum();
        Same("hisobot EndBalance", total, report.EndBalance);

        var debt = (await world.Admin.Send(new CustomerBalanceQuery { CustomerId = customerId })).Single();
        Same("Kontragentlar", total, debt.Creditor - debt.Debitor);

        var summary = await world.Admin.Send(new CustomerBalanceSummaryQuery { CustomerId = customerId });
        Same("Kontragentlar jami", total, summary.Creditor - summary.Debitor);

        return page;
    }

    public static async Task Balances(World world, long customerId, params (long Currency, decimal Balance)[] expected)
        => Same("kutilgan qoldiq", expected.ToDictionary(e => e.Currency, e => e.Balance), await Consistent(world, customerId));

    public static async Task Discount(World world, long customerId, decimal expected)
    {
        var customer = await world.Admin.Send(new GetCustomerByIdQuery(customerId));
        var debt = (await world.Admin.Send(new CustomerBalanceQuery { CustomerId = customerId })).Single();

        Same("mijoz chegirmasi", expected, customer.Accounts.Single().Discount);
        Same("Kontragentlar chegirmasi", expected, debt.Discount);
    }

    // SQL numeric masshtabi (-400000 va -400000.0) bilan farq qilishi mumkin, shuning uchun qiymatlar son sifatida solishtiriladi
    private static void Same(string reading, Dictionary<long, decimal> expected, Dictionary<long, decimal> actual)
        => Assert.True(
            expected.Count == actual.Count && expected.All(pair => actual.TryGetValue(pair.Key, out var value) && value == pair.Value),
            $"{reading}: [{Describe(actual)}], kutilgani [{Describe(expected)}]");

    private static string Describe(Dictionary<long, decimal> balances)
        => string.Join(", ", balances.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value:0.##}"));

    private static void Same(string reading, decimal expected, decimal actual)
        => Assert.True(expected == actual, $"{reading}: {actual}, kutilgani {expected}");
}
