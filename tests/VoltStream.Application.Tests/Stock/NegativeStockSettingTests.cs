namespace VoltStream.Application.Tests.Stock;

using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Settings.Commands;
using VoltStream.Application.Features.Settings.Queries;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Application.Tests.Ledger;

[Collection(DatabaseCollection.Name)]
public sealed class NegativeStockSettingTests(PostgresFixture db)
{
    // Sozlama butun bazaga ta'sir qiladi, shuning uchun uni o'zgartiradigan testlar o'z bazasida ishlaydi
    private async Task<(TestDatabase Database, World World, long Product, long Customer)> Strict(int rolls = 5)
    {
        var database = await db.NewDatabase();
        var world = await database.NewWorld();
        var product = await world.NewProduct();
        await world.Supply(product, rolls, 100, 1_000);
        await world.SetNegativeStock(false);

        return (database, world, product, await world.NewCustomer());
    }

    [Fact]
    public async Task By_default_selling_below_the_stock_is_allowed()
    {
        var w = await db.NewWorld();
        var product = await w.NewProduct();
        var customer = await w.NewCustomer();
        await w.Supply(product, 2, 100, 1_000);

        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 3, 100, 1_000)]));

        Assert.True((await w.Seller.Send(new GetGeneralSettingsQuery())).AllowNegativeStock);
        Assert.Equal([new StockRow(100, -1, -100)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -300_000));
    }

    [Fact]
    public async Task Selling_more_than_the_stock_is_rejected_without_a_trace()
    {
        var (database, w, product, customer) = await Strict(3);
        await using var _ = database;

        await Assert.ThrowsAsync<ConflictException>(() => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 5, 100, 1_000)])));

        Assert.Equal([new StockRow(100, 3, 300)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));
        Assert.Empty(await w.SalesOf(customer));
    }

    [Fact]
    public async Task Two_rows_of_one_sale_on_the_same_stock_row_count_together()
    {
        var (database, w, product, customer) = await Strict(3);
        await using var _ = database;

        await Assert.ThrowsAsync<ConflictException>(()
            => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 1_000), new Line(product, 2, 100, 1_000)])));

        Assert.Equal([new StockRow(100, 3, 300)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));
    }

    [Fact]
    public async Task Exactly_the_stock_can_be_sold_and_not_a_roll_more()
    {
        var (database, w, product, customer) = await Strict(3);
        await using var _ = database;

        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 3, 100, 1_000)]));
        Assert.Equal([new StockRow(100, 0, 0)], await w.Stock(product));

        await Assert.ThrowsAsync<ConflictException>(() => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 1_000)])));

        Assert.Equal([new StockRow(100, 0, 0)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -300_000));
    }

    [Fact]
    public async Task A_cut_is_allowed_while_the_whole_roll_is_there_and_its_remainder_can_be_sold_once()
    {
        var (database, w, product, customer) = await Strict(5);
        await using var _ = database;

        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 1_000, Length: 150)]));
        Assert.Equal([new StockRow(50, 1, 50), new StockRow(100, 3, 300)], await w.Stock(product));

        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 50, 1_000)]));
        await Assert.ThrowsAsync<ConflictException>(() => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 50, 1_000)])));

        Assert.Equal([new StockRow(50, 0, 0), new StockRow(100, 3, 300)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -(150 + 50) * 1_000));
    }

    [Fact]
    public async Task A_cut_of_a_roll_that_is_not_there_is_rejected()
    {
        var (database, w, product, customer) = await Strict(1);
        await using var _ = database;

        await Assert.ThrowsAsync<ConflictException>(() => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 1_000, Length: 150)])));

        Assert.Equal([new StockRow(100, 1, 100)], await w.Stock(product));
        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 1_000, Length: 70)]));
        Assert.Equal([new StockRow(30, 1, 30), new StockRow(100, 0, 0)], await w.Stock(product));
    }

    [Fact]
    public async Task Turning_the_setting_back_on_allows_overselling_again()
    {
        var (database, w, product, customer) = await Strict(2);
        await using var _ = database;
        await Assert.ThrowsAsync<ConflictException>(() => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 3, 100, 1_000)])));

        await w.SetNegativeStock(true);
        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 3, 100, 1_000)]));

        Assert.Equal([new StockRow(100, -1, -100)], await w.Stock(product));
    }

    [Fact]
    public async Task Editing_a_sale_gives_its_own_rolls_back_first()
    {
        var (database, w, product, customer) = await Strict(5);
        await using var _ = database;
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 3, 100, 1_000)]));

        await w.Admin.Send(World.Edit(saleId, w.Sale(customer, w.Uzs, [new Line(product, 3, 100, 1_000)])));
        Assert.Equal([new StockRow(100, 2, 200)], await w.Stock(product));

        await w.Admin.Send(World.Edit((await w.LastSale(customer)).Id, w.Sale(customer, w.Uzs, [new Line(product, 5, 100, 1_000)])));

        Assert.Equal([new StockRow(100, 0, 0)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -500_000));
    }

    [Fact]
    public async Task An_edit_that_does_not_fit_is_rejected_and_the_sale_stays_as_it_was()
    {
        var (database, w, product, customer) = await Strict(5);
        await using var _ = database;
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 3, 100, 1_000)]));

        await Assert.ThrowsAsync<ConflictException>(()
            => w.Admin.Send(World.Edit(saleId, w.Sale(customer, w.Uzs, [new Line(product, 6, 100, 1_000)]))));

        Assert.Equal(saleId, (await w.LastSale(customer)).Id);
        Assert.Equal([new StockRow(100, 2, 200)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -300_000));
    }

    [Fact]
    public async Task Only_settings_general_may_change_the_setting_but_everyone_may_read_it()
    {
        await using var database = await db.NewDatabase();
        var w = await database.NewWorld();

        foreach (var actor in new[] { w.Seller, w.Cashier, w.Manager })
            await Assert.ThrowsAsync<ForbiddenException>(() => actor.Send(new UpdateGeneralSettingsCommand(false)));
        Assert.True((await w.Seller.Send(new GetGeneralSettingsQuery())).AllowNegativeStock);

        await w.As(TestUser.With(w.Branch, Permissions.SettingsGeneral)).Send(new UpdateGeneralSettingsCommand(false));
        foreach (var actor in new[] { w.Seller, w.Cashier, w.Manager, w.Admin })
            Assert.False((await actor.Send(new GetGeneralSettingsQuery())).AllowNegativeStock);

        await w.Admin.Send(new UpdateGeneralSettingsCommand(true));
        Assert.True((await w.Seller.Send(new GetGeneralSettingsQuery())).AllowNegativeStock);
    }
}
