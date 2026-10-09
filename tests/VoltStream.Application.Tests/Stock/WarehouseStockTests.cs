namespace VoltStream.Application.Tests.Stock;

using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Features.Sales.Queries;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Application.Tests.Ledger;

[Collection(DatabaseCollection.Name)]
public sealed class WarehouseStockTests(PostgresFixture db)
{
    private async Task<(World World, long Product, long Customer)> Arrange(int rolls = 5)
    {
        var world = await db.NewWorld();
        var product = await world.NewProduct();
        await world.Supply(product, rolls, lengthPerRoll: 100, price: 5_000);

        return (world, product, await world.NewCustomer());
    }

    [Fact]
    public async Task Supply_joins_the_matching_roll_length_and_opens_a_row_for_another()
    {
        var w = await db.NewWorld();
        var product = await w.NewProduct();

        await w.Supply(product, 5, 100, 5_000);
        await w.Supply(product, 3, 100, 5_000);
        await w.Supply(product, 2, 50, 5_000);

        Assert.Equal([new StockRow(50, 2, 100), new StockRow(100, 8, 800)], await w.Stock(product));
    }

    [Fact]
    public async Task Sale_takes_whole_rolls_out_of_the_stock()
    {
        var (w, product, customer) = await Arrange();

        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));

        Assert.Equal([new StockRow(100, 3, 300)], await w.Stock(product));
    }

    [Fact]
    public async Task Cut_sale_returns_the_remainder_to_the_stock_as_a_piece()
    {
        var (w, product, customer) = await Arrange();

        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000, Length: 150)]));

        Assert.Equal([new StockRow(50, 1, 50), new StockRow(100, 3, 300)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -750_000));
    }

    [Fact]
    public async Task Second_cut_with_the_same_remainder_joins_the_existing_piece()
    {
        var (w, product, customer) = await Arrange();

        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000, Length: 150)]));
        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000, Length: 150)]));

        Assert.Equal([new StockRow(50, 2, 100), new StockRow(100, 1, 100)], await w.Stock(product));
    }

    [Fact]
    public async Task Deleting_a_sale_returns_the_rolls()
    {
        var (w, product, customer) = await Arrange();
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));

        await w.DeleteSale(saleId);

        Assert.Equal([new StockRow(100, 5, 500)], await w.Stock(product));
    }

    [Fact]
    public async Task Deleting_a_cut_sale_returns_the_rolls_and_removes_the_piece()
    {
        var (w, product, customer) = await Arrange();
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000, Length: 150)]));

        await w.DeleteSale(saleId);

        Assert.Equal([new StockRow(100, 5, 500)], await w.Stock(product));
    }

    [Fact]
    public async Task Deleting_a_cut_sale_after_its_piece_was_sold_keeps_the_sold_length_out_of_the_stock()
    {
        var (w, product, customer) = await Arrange();
        var cutSale = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000, Length: 150)]));
        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 50, 5_000)]));

        await w.DeleteSale(cutSale);

        var stock = await w.Stock(product);
        Assert.Equal(450, stock.Sum(row => row.Length));
    }

    [Fact]
    public async Task Editing_a_sale_leaves_only_the_final_quantities_out_of_the_stock()
    {
        var (w, product, customer) = await Arrange();
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));

        await w.Admin.Send(World.Edit(saleId, w.Sale(customer, w.Uzs, [new Line(product, 3, 100, 5_000)])));

        Assert.Equal([new StockRow(100, 2, 200)], await w.Stock(product));
    }

    [Fact]
    public async Task Oversell_is_allowed_and_leaves_the_stock_negative()
    {
        var (w, product, customer) = await Arrange();

        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 7, 100, 5_000)]));

        Assert.Equal([new StockRow(100, -2, -200)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -3_500_000));
    }

    [Fact]
    public async Task Selling_a_roll_length_that_was_never_supplied_is_rejected_without_a_trace()
    {
        var (w, product, customer) = await Arrange();

        await Assert.ThrowsAsync<NotFoundException>(()
            => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 70, 5_000)])));

        Assert.Equal([new StockRow(100, 5, 500)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));
        Assert.Empty(await w.Admin.Send(new SaleFilterQuery { Filters = new() { ["customerId"] = [customer.ToString()] } }));
    }

    [Fact]
    public async Task Stock_is_kept_per_branch()
    {
        var (w, product, customer) = await Arrange();
        await w.Supply(product, 5, 100, 5_000, branchId: w.OtherBranch);

        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));

        Assert.Equal([new StockRow(100, 3, 300)], await w.Stock(product, w.Branch));
        Assert.Equal([new StockRow(100, 5, 500)], await w.Stock(product, w.OtherBranch));
    }
}
