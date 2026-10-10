namespace VoltStream.Application.Tests.Security;

using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Features.Branches.Commands;
using VoltStream.Application.Features.CustomerOperations.Commands;
using VoltStream.Application.Features.Sales.Queries;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Application.Tests.Ledger;

[Collection(DatabaseCollection.Name)]
public sealed class PermissionTests(PostgresFixture db)
{
    private async Task<(World World, long Product, long Customer, long Sale)> Arrange()
    {
        var world = await db.NewWorld();
        var product = await world.NewProduct();
        await world.Supply(product, 10, 100, 5_000);
        var customer = await world.NewCustomer();
        var sale = await world.Admin.Send(world.Sale(customer, world.Uzs, [new Line(product, 2, 100, 5_000)]));

        return (world, product, customer, sale);
    }

    [Fact]
    public async Task A_seller_cannot_take_payments_edit_or_delete_sales()
    {
        var (w, product, customer, sale) = await Arrange();
        var operationId = (await w.Admin.Send(new GetSaleByIdQuery(sale))).CustomerOperationId;

        await Assert.ThrowsAsync<ForbiddenException>(() => w.Seller.Send(World.Payment(customer, w.Uzs, 300_000)));
        await Assert.ThrowsAsync<ForbiddenException>(()
            => w.Seller.Send(World.Edit(sale, w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)]))));
        await Assert.ThrowsAsync<ForbiddenException>(() => w.Seller.Send(new DeleteCustomerOperationCommand(operationId)));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_000_000));
        Assert.Equal([new StockRow(100, 8, 800)], await w.Stock(product));
    }

    [Fact]
    public async Task A_cashier_takes_payments_but_cannot_sell()
    {
        var (w, product, customer, _) = await Arrange();

        await Assert.ThrowsAsync<ForbiddenException>(()
            => w.Cashier.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)])));
        await w.Cashier.Send(World.Payment(customer, w.Uzs, 300_000));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -700_000));
        Assert.Equal([new StockRow(100, 8, 800)], await w.Stock(product));
    }

    [Fact]
    public async Task A_manager_sells_and_edits_but_cannot_change_settings()
    {
        var (w, product, customer, sale) = await Arrange();

        await w.Manager.Send(World.Edit(sale, w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)])));
        await Assert.ThrowsAsync<ForbiddenException>(() => w.Manager.Send(new CreateBranchCommand("closed to managers")));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -500_000));
    }
}
