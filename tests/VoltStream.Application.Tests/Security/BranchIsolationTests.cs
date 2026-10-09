namespace VoltStream.Application.Tests.Security;

using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.CustomerOperations.Commands;
using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Application.Features.Sales.Queries;
using VoltStream.Application.Features.Stats.Queries;
using VoltStream.Application.Features.WarehouseStocks.Queries;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Application.Tests.Ledger;
using VoltStream.Domain.Enums;

[Collection(DatabaseCollection.Name)]
public sealed class BranchIsolationTests(PostgresFixture db)
{
    private async Task<(World World, long Product, long Customer)> Arrange()
    {
        var world = await db.NewWorld();
        var product = await world.NewProduct();
        await world.Supply(product, 10, 100, 5_000);
        await world.Supply(product, 10, 100, 5_000, branchId: world.OtherBranch);

        return (world, product, await world.NewCustomer());
    }

    private static long[] Ids(IEnumerable<SaleDto> sales) => [.. sales.Select(sale => sale.Id).Order()];

    [Fact]
    public async Task Another_branch_cannot_read_edit_or_delete_a_sale()
    {
        var (w, product, customer) = await Arrange();
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        var operationId = (await w.Admin.Send(new GetSaleByIdQuery(saleId))).CustomerOperationId;

        await Assert.ThrowsAsync<ForbiddenException>(() => w.OtherManager.Send(new GetSaleByIdQuery(saleId)));
        await Assert.ThrowsAsync<ForbiddenException>(() => w.OtherManager.Send(new GetSaleReceiptQuery(saleId)));
        await Assert.ThrowsAsync<ForbiddenException>(()
            => w.OtherManager.Send(World.Edit(saleId, w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)]))));
        await Assert.ThrowsAsync<ForbiddenException>(() => w.OtherManager.Send(new DeleteCustomerOperationCommand(operationId)));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_000_000));
        Assert.Equal([new StockRow(100, 8, 800)], await w.Stock(product));
    }

    [Fact]
    public async Task A_sale_by_a_branch_employee_always_lands_in_their_own_branch()
    {
        var (w, product, customer) = await Arrange();

        var saleId = await w.Seller.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100)], branchId: w.OtherBranch));

        Assert.Equal(w.Branch, (await w.Admin.Send(new GetSaleByIdQuery(saleId))).BranchId);
        Assert.Equal([new StockRow(100, 8, 800)], await w.Stock(product, w.Branch));
        Assert.Equal([new StockRow(100, 10, 1_000)], await w.Stock(product, w.OtherBranch));
    }

    [Fact]
    public async Task Admin_picks_the_branch_with_each_sale()
    {
        var (w, product, customer) = await Arrange();

        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)], branchId: w.OtherBranch));

        Assert.Equal(w.OtherBranch, (await w.Admin.Send(new GetSaleByIdQuery(saleId))).BranchId);
        Assert.Equal([new StockRow(100, 10, 1_000)], await w.Stock(product, w.Branch));
        Assert.Equal([new StockRow(100, 8, 800)], await w.Stock(product, w.OtherBranch));
    }

    [Fact]
    public async Task An_employee_without_a_branch_cannot_sell()
    {
        var (w, product, customer) = await Arrange();
        var drifter = w.As(TestUser.With(null, Permissions.SalesCreate));

        await Assert.ThrowsAsync<ForbiddenException>(() => drifter.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100)])));

        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));
    }

    [Fact]
    public async Task Lists_show_only_the_readers_own_branch()
    {
        var (w, product, customer) = await Arrange();
        var own = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)]));
        var other = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)], branchId: w.OtherBranch));

        Assert.Equal([other], Ids(await w.OtherManager.Send(new SaleFilterQuery { BranchId = w.Branch })));
        Assert.Equal([own], Ids(await w.Manager.Send(new SaleFilterQuery { BranchId = w.OtherBranch })));
        Assert.Equal([other], Ids(await w.OtherManager.Send(new GetAllSalesQuery())));

        SaleFilterQuery ForCustomer() => new() { Filters = new() { ["customerId"] = [customer.ToString()] } };
        Assert.Equal([own, other], Ids(await w.Admin.Send(ForCustomer())));
        Assert.Equal([other], Ids(await w.Admin.Send(ForCustomer() with { BranchId = w.OtherBranch })));
    }

    [Fact]
    public async Task Stock_lists_show_only_the_readers_own_branch()
    {
        var (w, _, _) = await Arrange();

        var stocks = await w.OtherSeller.Send(new GetAllWarehouseStockQuery(w.Branch));

        Assert.NotEmpty(stocks);
        Assert.All(stocks, row => Assert.Equal(w.OtherBranch, row.BranchId));
    }

    [Fact]
    public async Task Without_report_permission_an_employee_sees_only_their_own_sales()
    {
        var (w, product, customer) = await Arrange();
        var colleague = w.As(TestUser.Of(UserRole.Seller, w.Branch));
        var mine = await w.Seller.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100)]));
        var theirs = await colleague.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100)]));

        Assert.Equal([mine], Ids(await w.Seller.Send(new SaleFilterQuery())));
        Assert.Equal([theirs], Ids(await colleague.Send(new SaleFilterQuery())));
        Assert.Equal([mine, theirs], Ids(await w.Manager.Send(new SaleFilterQuery())));

        await Assert.ThrowsAsync<ForbiddenException>(() => colleague.Send(new GetSaleReceiptQuery(mine)));
        Assert.Equal(mine, (await w.Manager.Send(new GetSaleReceiptQuery(mine))).SaleId);
    }

    [Fact]
    public async Task An_employee_cannot_ask_for_a_colleagues_numbers()
    {
        var (w, product, customer) = await Arrange();
        var colleague = w.As(TestUser.Of(UserRole.Seller, w.Branch));
        await w.Seller.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100)]));
        await colleague.Send(w.Sale(customer, w.Uzs, [new Line(product, 3, 100)]));
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow.AddDays(1);

        Assert.Equal(3, (await colleague.Send(new GetBranchStatsQuery(from, to, UserId: w.Seller.User.UserId))).SoldRolls);
        Assert.Equal(2, (await w.Manager.Send(new GetBranchStatsQuery(from, to, UserId: w.Seller.User.UserId))).SoldRolls);
        Assert.Equal(5, (await w.Manager.Send(new GetBranchStatsQuery(from, to))).SoldRolls);
    }
}
