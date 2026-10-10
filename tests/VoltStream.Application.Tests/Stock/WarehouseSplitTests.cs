namespace VoltStream.Application.Tests.Stock;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Features.CustomerOperations.Commands;
using VoltStream.Application.Features.Sales.Queries;
using VoltStream.Application.Features.Supplies.Commands;
using VoltStream.Application.Features.WarehouseStocks.Queries;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Application.Tests.Ledger;
using VoltStream.Domain.Enums;

[Collection(DatabaseCollection.Name)]
public sealed class WarehouseSplitTests(PostgresFixture db)
{
    private async Task<(World World, long Warehouse, long Second, long Product, long Customer)> Shared()
    {
        var world = await db.NewWorld();
        var warehouse = await world.WarehouseOf(world.Branch);

        return (world, warehouse, await world.NewBranch(warehouse), await world.NewProduct(), await world.NewCustomer());
    }

    private async Task<(World World, long Old, long Fresh, long Product, long Customer)> Switchable()
    {
        var world = await db.NewWorld();

        return (world, await world.WarehouseOf(world.Branch), await world.NewWarehouse(), await world.NewProduct(), await world.NewCustomer());
    }

    private static async Task<List<(long Id, long Warehouse, int Rolls, decimal Length)>> Listed(Actor actor, long product)
        => [.. (await actor.Send(new GetAllWarehouseStockQuery()))
            .Where(stock => stock.Product.Id == product)
            .OrderBy(stock => stock.Id)
            .Select(stock => (stock.Id, stock.WarehouseId, stock.RollCount, stock.TotalLength))];

    [Fact]
    public async Task Supplies_of_both_branches_land_in_one_stock_row()
    {
        var (w, warehouse, second, product, _) = await Shared();

        await w.Supply(product, 5, 100, 5_000);
        await w.Supply(product, 3, 100, 5_000, branchId: second);

        Assert.Equal([new StockRow(100, 8, 800)], await w.StockIn(warehouse, product));
        Assert.Equal([new StockRow(100, 8, 800)], await w.Stock(product, w.Branch));
        Assert.Equal([new StockRow(100, 8, 800)], await w.Stock(product, second));
    }

    [Fact]
    public async Task Sales_of_both_branches_take_from_the_same_row()
    {
        var (w, warehouse, second, product, customer) = await Shared();
        await w.Supply(product, 10, 100, 5_000);
        var secondSeller = w.As(TestUser.Of(UserRole.Seller, second));

        var first = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        var other = await secondSeller.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100)], branchId: second));

        Assert.Equal([new StockRow(100, 7, 700)], await w.Stock(product, w.Branch));
        Assert.Equal([new StockRow(100, 7, 700)], await w.Stock(product, second));
        Assert.Equal(warehouse, await w.WarehouseOfSale(first));
        Assert.Equal(warehouse, await w.WarehouseOfSale(other));
        Assert.Equal(w.Branch, (await w.Admin.Send(new GetSaleByIdQuery(first))).BranchId);
        Assert.Equal(second, (await w.Admin.Send(new GetSaleByIdQuery(other))).BranchId);
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_500_000));
    }

    [Fact]
    public async Task Employees_of_both_branches_list_the_same_stock_rows()
    {
        var (w, warehouse, second, product, _) = await Shared();
        await w.Supply(product, 5, 100, 5_000);
        await w.Supply(product, 2, 50, 5_000, branchId: second);
        var secondSeller = w.As(TestUser.Of(UserRole.Seller, second));

        var first = await Listed(w.Seller, product);

        Assert.Equal(2, first.Count);
        Assert.Equal(first, await Listed(secondSeller, product));
        Assert.All(first, row => Assert.Equal(warehouse, row.Warehouse));
    }

    [Fact]
    public async Task Documents_stay_per_branch_while_the_stock_is_shared()
    {
        var (w, _, second, product, customer) = await Shared();
        await w.Supply(product, 10, 100, 5_000);
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        var operationId = (await w.Admin.Send(new GetSaleByIdQuery(saleId))).CustomerOperationId;
        var neighbour = w.As(TestUser.Of(UserRole.Manager, second));

        await Assert.ThrowsAsync<ForbiddenException>(() => neighbour.Send(new GetSaleByIdQuery(saleId)));
        await Assert.ThrowsAsync<ForbiddenException>(() => neighbour.Send(new DeleteCustomerOperationCommand(operationId)));
        Assert.Empty(await neighbour.Send(new SaleFilterQuery()));
        Assert.Equal([new StockRow(100, 8, 800)], await w.Stock(product, second));
    }

    [Fact]
    public async Task Cut_remainder_is_visible_from_both_branches_and_goes_away_with_the_sale()
    {
        var (w, _, second, product, customer) = await Shared();
        await w.Supply(product, 5, 100, 5_000);
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000, Length: 150)]));

        Assert.Equal([new StockRow(50, 1, 50), new StockRow(100, 3, 300)], await w.Stock(product, second));

        await w.DeleteSale(saleId);

        Assert.Equal([new StockRow(100, 5, 500)], await w.Stock(product, second));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));
    }

    [Fact]
    public async Task Deleting_a_sale_made_before_the_switch_returns_the_rolls_to_the_old_warehouse()
    {
        var (w, old, fresh, product, customer) = await Switchable();
        await w.Supply(product, 5, 100, 5_000);
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        await w.SwitchWarehouse(w.Branch, fresh);

        await w.DeleteSale(saleId);

        Assert.Equal([new StockRow(100, 5, 500)], await w.StockIn(old, product));
        Assert.Empty(await w.StockIn(fresh, product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));
    }

    [Fact]
    public async Task Editing_a_sale_made_before_the_switch_keeps_it_in_the_old_warehouse()
    {
        var (w, old, fresh, product, customer) = await Switchable();
        await w.Supply(product, 5, 100, 5_000);
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        await w.SwitchWarehouse(w.Branch, fresh);

        await w.Admin.Send(World.Edit(saleId, w.Sale(customer, w.Uzs, [new Line(product, 3, 100, 5_000)])));

        Assert.Equal([new StockRow(100, 2, 200)], await w.StockIn(old, product));
        Assert.Empty(await w.StockIn(fresh, product));
        Assert.Equal(old, await w.WarehouseOfSale((await w.LastSale(customer)).Id));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_500_000));
    }

    [Fact]
    public async Task A_sale_made_after_the_switch_takes_from_the_new_warehouse()
    {
        var (w, old, fresh, product, customer) = await Switchable();
        await w.Supply(product, 5, 100, 5_000);
        await w.SwitchWarehouse(w.Branch, fresh);
        await w.Supply(product, 4, 100, 5_000);

        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)]));

        Assert.Equal([new StockRow(100, 3, 300)], await w.StockIn(fresh, product));
        Assert.Equal([new StockRow(100, 5, 500)], await w.StockIn(old, product));
        Assert.Equal([new StockRow(100, 3, 300)], await w.Stock(product));
        Assert.Equal(fresh, await w.WarehouseOfSale(saleId));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -500_000));
    }

    [Fact]
    public async Task Stock_left_in_the_old_warehouse_cannot_be_sold_after_the_switch()
    {
        var (w, old, fresh, product, customer) = await Switchable();
        await w.Supply(product, 5, 100, 5_000);
        await w.SwitchWarehouse(w.Branch, fresh);

        await Assert.ThrowsAsync<NotFoundException>(() => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)])));

        Assert.Equal([new StockRow(100, 5, 500)], await w.StockIn(old, product));
        Assert.Empty(await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));
    }

    [Fact]
    public async Task Deleting_a_supply_made_before_the_switch_takes_it_out_of_the_old_warehouse()
    {
        var (w, old, fresh, product, _) = await Switchable();
        var supplyId = await w.Supply(product, 5, 100, 5_000);
        await w.SwitchWarehouse(w.Branch, fresh);
        await w.Supply(product, 4, 100, 5_000);

        await w.Admin.Send(new DeleteSupplyCommand(supplyId));

        Assert.Equal([new StockRow(100, 0, 0)], await w.StockIn(old, product));
        Assert.Equal([new StockRow(100, 4, 400)], await w.StockIn(fresh, product));
    }

    [Fact]
    public async Task Editing_a_supply_made_before_the_switch_changes_the_old_warehouse()
    {
        var (w, old, fresh, product, _) = await Switchable();
        var supplyId = await w.Supply(product, 5, 100, 5_000);
        await w.SwitchWarehouse(w.Branch, fresh);
        await w.Supply(product, 4, 100, 5_000);

        await w.Admin.Send(w.EditSupply(supplyId, product, 7, 100, 5_000));

        Assert.Equal([new StockRow(100, 7, 700)], await w.StockIn(old, product));
        Assert.Equal([new StockRow(100, 4, 400)], await w.StockIn(fresh, product));
        Assert.Equal(old, await w.WarehouseOfSupply(supplyId));
    }

    [Fact]
    public async Task Supply_remembers_the_warehouse_it_was_made_into()
    {
        var (w, old, fresh, product, _) = await Switchable();
        var before = await w.Supply(product, 5, 100, 5_000);
        await w.SwitchWarehouse(w.Branch, fresh);
        var after = await w.Supply(product, 4, 100, 5_000);

        Assert.Equal(old, await w.WarehouseOfSupply(before));
        Assert.Equal(fresh, await w.WarehouseOfSupply(after));
    }

    [Fact]
    public async Task A_document_without_a_stored_warehouse_falls_back_to_the_branch_warehouse()
    {
        var (w, old, _, product, customer) = await Switchable();
        var supplyId = await w.Supply(product, 5, 100, 5_000);
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        await w.Db.Write(async context =>
        {
            await context.Sales.Where(x => x.Id == saleId).ExecuteUpdateAsync(s => s.SetProperty(x => x.WarehouseId, (long?)null));
            await context.Supplies.Where(x => x.Id == supplyId).ExecuteUpdateAsync(s => s.SetProperty(x => x.WarehouseId, (long?)null));
        });

        await w.DeleteSale(saleId);
        await w.Admin.Send(new DeleteSupplyCommand(supplyId));

        Assert.Equal([new StockRow(100, 0, 0)], await w.StockIn(old, product));
    }
}
