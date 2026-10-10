namespace VoltStream.Application.Tests.Stock;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Application.Tests.Ledger;

// Poyga natijasi tasodifiy: nechta amal yutishi noma'lum, shuning uchun tekshiruv yutganlar soniga nisbatan, oxirida yutqazganlar qayta uriniladi
[Collection(DatabaseCollection.Name)]
public sealed class StockConcurrencyTests(PostgresFixture db)
{
    private sealed record Outcome(Func<Task> Operation, Exception? Error);

    private static Task<Outcome[]> Race(Func<Task> operation, int times)
        => Race(Enumerable.Repeat(operation, times));

    private static Task<Outcome[]> Race(IEnumerable<Func<Task>> operations)
        => Task.WhenAll(operations.Select(async operation =>
        {
            try
            {
                await operation();
                return new Outcome(operation, null);
            }
            catch (Exception ex)
            {
                return new Outcome(operation, ex);
            }
        }));

    private static Outcome[] Lost(Outcome[] outcomes) => [.. outcomes.Where(outcome => outcome.Error is not null)];

    private static int Won(Outcome[] outcomes) => outcomes.Count(outcome => outcome.Error is null);

    [Fact]
    public async Task Racing_sales_on_one_row_never_lose_an_update()
    {
        var w = await db.NewWorld();
        var product = await w.NewProduct();
        var customer = await w.NewCustomer();
        await w.Supply(product, 100, 100, 1_000);

        var outcomes = await Race(() => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 1_000)])), 10);

        var won = Won(outcomes);
        Assert.True(won >= 1);
        Assert.All(Lost(outcomes), outcome => Assert.IsType<DbUpdateConcurrencyException>(outcome.Error));
        Assert.Equal([new StockRow(100, 100 - won, (100 - won) * 100)], await w.Stock(product));
        Assert.Equal(won, (await w.SalesOf(customer)).Count);
        await LedgerAssert.Balances(w, customer, (w.Uzs, -won * 100_000));

        foreach (var lost in Lost(outcomes))
            await lost.Operation();

        Assert.Equal([new StockRow(100, 90, 9_000)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_000_000));
    }

    [Fact]
    public async Task Racing_sales_never_push_a_strict_stock_below_zero()
    {
        await using var database = await db.NewDatabase();
        var w = await database.NewWorld();
        var product = await w.NewProduct();
        var customer = await w.NewCustomer();
        await w.Supply(product, 4, 100, 1_000);
        await w.SetNegativeStock(false);

        var outcomes = await Race(() => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 1_000)])), 10);

        var won = Won(outcomes);
        Assert.InRange(won, 1, 4);
        Assert.All(Lost(outcomes), outcome => Assert.True(outcome.Error is ConflictException or DbUpdateConcurrencyException, outcome.Error?.ToString()));
        Assert.Equal([new StockRow(100, 4 - won, (4 - won) * 100)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -won * 100_000));

        foreach (var lost in Lost(outcomes))
            try
            {
                await lost.Operation();
            }
            catch (ConflictException)
            {
            }

        Assert.Equal([new StockRow(100, 0, 0)], await w.Stock(product));
        Assert.Equal(4, (await w.SalesOf(customer)).Count);
        await LedgerAssert.Balances(w, customer, (w.Uzs, -400_000));
    }

    [Fact]
    public async Task Racing_sale_deletions_return_every_roll_exactly_once()
    {
        var w = await db.NewWorld();
        var product = await w.NewProduct();
        var customer = await w.NewCustomer();
        await w.Supply(product, 20, 100, 1_000);
        var sales = new List<long>();
        for (var i = 0; i < 6; i++)
            sales.Add(await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 1_000)])));

        var outcomes = await Race(sales.Select<long, Func<Task>>(saleId => () => w.DeleteSale(saleId)));

        var deleted = Won(outcomes);
        Assert.All(Lost(outcomes), outcome => Assert.IsType<DbUpdateConcurrencyException>(outcome.Error));
        Assert.Equal([new StockRow(100, 14 + deleted, (14 + deleted) * 100)], await w.Stock(product));
        Assert.Equal(6 - deleted, (await w.SalesOf(customer)).Count);
        await LedgerAssert.Balances(w, customer, (w.Uzs, -(6 - deleted) * 100_000));

        foreach (var lost in Lost(outcomes))
            await lost.Operation();

        Assert.Equal([new StockRow(100, 20, 2_000)], await w.Stock(product));
        Assert.Empty(await w.SalesOf(customer));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));
    }

    [Fact]
    public async Task Racing_supplies_and_sales_on_one_row_never_lose_an_update()
    {
        var w = await db.NewWorld();
        var product = await w.NewProduct();
        var customer = await w.NewCustomer();
        await w.Supply(product, 20, 100, 1_000);
        Func<Task> supply = () => w.Supply(product, 2, 100, 1_000);
        Func<Task> sale = () => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 1_000)]));

        var outcomes = await Race([.. Enumerable.Repeat(supply, 5), .. Enumerable.Repeat(sale, 5)]);

        var suppliesWon = outcomes.Count(outcome => outcome.Operation == supply && outcome.Error is null);
        var salesWon = outcomes.Count(outcome => outcome.Operation == sale && outcome.Error is null);
        var rolls = 20 + 2 * suppliesWon - salesWon;
        Assert.All(Lost(outcomes), outcome => Assert.IsType<DbUpdateConcurrencyException>(outcome.Error));
        Assert.Equal([new StockRow(100, rolls, rolls * 100)], await w.Stock(product));
        Assert.Equal(salesWon, (await w.SalesOf(customer)).Count);
        await LedgerAssert.Balances(w, customer, (w.Uzs, -salesWon * 100_000));

        foreach (var lost in Lost(outcomes))
            await lost.Operation();

        Assert.Equal([new StockRow(100, 25, 2_500)], await w.Stock(product));
        Assert.Equal(5, (await w.SalesOf(customer)).Count);
        await LedgerAssert.Balances(w, customer, (w.Uzs, -500_000));
    }
}
