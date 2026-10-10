namespace VoltStream.Application.Tests.Stock;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Features.Branches.Commands;
using VoltStream.Application.Features.Branches.Queries;
using VoltStream.Application.Features.Warehouses.Commands;
using VoltStream.Application.Features.Warehouses.Queries;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Domain.Enums;

[Collection(DatabaseCollection.Name)]
public sealed class WarehouseManagementTests(PostgresFixture db)
{
    private static async Task<long[]> Listed(Actor actor)
        => [.. (await actor.Send(new GetAllWarehouseQuery())).Select(warehouse => warehouse.Id).Order()];

    // Hech bir filialga biriktirilmagan, lekin qoldig'i va hujjatlari (kirim + savdo) bor ombor
    private async Task<(World World, long Warehouse)> Parked()
    {
        var world = await db.NewWorld();
        var warehouse = await world.NewWarehouse();
        var branch = await world.NewBranch(warehouse);
        var product = await world.NewProduct();
        await world.Supply(product, 5, 100, 5_000, branchId: branch);
        await world.Admin.Send(world.Sale(await world.NewCustomer(), world.Uzs, [new Line(product, 2, 100, 5_000)], branchId: branch));
        await world.SwitchWarehouse(branch, await world.WarehouseOf(world.Branch));

        return (world, warehouse);
    }

    [Fact]
    public async Task A_branch_employee_cannot_open_another_branchs_warehouse()
    {
        var w = await db.NewWorld();
        var own = await w.WarehouseOf(w.Branch);
        var other = await w.WarehouseOf(w.OtherBranch);

        Assert.Equal(own, (await w.Seller.Send(new GetWarehouseByIdQuery(own))).Id);
        Assert.Equal(other, (await w.OtherManager.Send(new GetWarehouseByIdQuery(other))).Id);
        await Assert.ThrowsAsync<ForbiddenException>(() => w.Seller.Send(new GetWarehouseByIdQuery(other)));
        await Assert.ThrowsAsync<ForbiddenException>(() => w.OtherManager.Send(new GetWarehouseByIdQuery(own)));

        Assert.Equal(own, (await w.Admin.Send(new GetWarehouseByIdQuery(own))).Id);
        Assert.Equal(other, (await w.Admin.Send(new GetWarehouseByIdQuery(other))).Id);
    }

    [Fact]
    public async Task The_warehouse_list_shows_only_the_readers_own_warehouse_and_admin_sees_all()
    {
        var w = await db.NewWorld();
        var own = await w.WarehouseOf(w.Branch);
        var other = await w.WarehouseOf(w.OtherBranch);

        Assert.Equal([own], await Listed(w.Seller));
        Assert.Equal([own], await Listed(w.Manager));
        Assert.Equal([other], await Listed(w.OtherSeller));

        var all = await Listed(w.Admin);
        Assert.Contains(own, all);
        Assert.Contains(other, all);
    }

    [Fact]
    public async Task Branches_sharing_a_warehouse_open_the_same_one_and_nobody_else_does()
    {
        var w = await db.NewWorld();
        var shared = await w.WarehouseOf(w.Branch);
        var second = w.As(TestUser.Of(UserRole.Seller, await w.NewBranch(shared)));

        Assert.Equal(shared, (await second.Send(new GetWarehouseByIdQuery(shared))).Id);
        Assert.Equal([shared], await Listed(second));
        await Assert.ThrowsAsync<ForbiddenException>(() => w.OtherSeller.Send(new GetWarehouseByIdQuery(shared)));
    }

    [Fact]
    public async Task Access_follows_the_branch_to_its_new_warehouse()
    {
        var w = await db.NewWorld();
        var old = await w.WarehouseOf(w.Branch);
        var fresh = await w.NewWarehouse();

        await w.SwitchWarehouse(w.Branch, fresh);

        Assert.Equal(fresh, (await w.Seller.Send(new GetWarehouseByIdQuery(fresh))).Id);
        Assert.Equal([fresh], await Listed(w.Seller));
        await Assert.ThrowsAsync<ForbiddenException>(() => w.Seller.Send(new GetWarehouseByIdQuery(old)));
    }

    [Fact]
    public async Task A_warehouse_with_a_branch_cannot_be_deleted()
    {
        var w = await db.NewWorld();
        var warehouse = await w.WarehouseOf(w.Branch);

        await Assert.ThrowsAsync<ConflictException>(() => w.Admin.Send(new DeleteWarehouseCommand(warehouse)));

        Assert.Contains(warehouse, await Listed(w.Admin));
    }

    [Fact]
    public async Task A_warehouse_with_stock_cannot_be_deleted()
    {
        var (w, warehouse) = await Parked();

        await Assert.ThrowsAsync<ConflictException>(() => w.Admin.Send(new DeleteWarehouseCommand(warehouse)));

        Assert.Contains(warehouse, await Listed(w.Admin));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Documents_alone_keep_a_warehouse_from_being_deleted(bool salesGoFirst)
    {
        var (w, warehouse) = await Parked();
        await w.Db.Write(context => context.WarehouseStocks.Where(stock => stock.WarehouseId == warehouse).ExecuteDeleteAsync());
        Task RemoveSales() => w.Db.Write(context => context.Sales.Where(sale => sale.WarehouseId == warehouse).ExecuteDeleteAsync());
        Task RemoveSupplies() => w.Db.Write(context => context.Supplies.Where(supply => supply.WarehouseId == warehouse).ExecuteDeleteAsync());

        await Assert.ThrowsAsync<ConflictException>(() => w.Admin.Send(new DeleteWarehouseCommand(warehouse)));

        await (salesGoFirst ? RemoveSales() : RemoveSupplies());
        await Assert.ThrowsAsync<ConflictException>(() => w.Admin.Send(new DeleteWarehouseCommand(warehouse)));

        await (salesGoFirst ? RemoveSupplies() : RemoveSales());
        Assert.True(await w.Admin.Send(new DeleteWarehouseCommand(warehouse)));
    }

    [Fact]
    public async Task An_empty_unattached_warehouse_is_deleted()
    {
        var w = await db.NewWorld();
        var warehouse = await w.NewWarehouse();
        Assert.Contains(warehouse, await Listed(w.Admin));

        Assert.True(await w.Admin.Send(new DeleteWarehouseCommand(warehouse)));

        Assert.DoesNotContain(warehouse, await Listed(w.Admin));
    }

    [Fact]
    public async Task A_warehouse_freed_by_a_switch_can_be_deleted_once_empty()
    {
        var w = await db.NewWorld();
        var branch = await w.NewBranch();
        var freed = await w.WarehouseOf(branch);
        await Assert.ThrowsAsync<ConflictException>(() => w.Admin.Send(new DeleteWarehouseCommand(freed)));

        await w.SwitchWarehouse(branch, await w.WarehouseOf(w.Branch));

        Assert.True(await w.Admin.Send(new DeleteWarehouseCommand(freed)));
    }

    [Fact]
    public async Task A_branch_cannot_be_given_a_missing_or_deleted_warehouse()
    {
        var w = await db.NewWorld();
        var before = await w.WarehouseOf(w.Branch);
        var gone = await w.NewWarehouse();
        await w.Admin.Send(new DeleteWarehouseCommand(gone));

        await Assert.ThrowsAsync<NotFoundException>(() => w.Admin.Send(new CreateBranchCommand($"branch-{Guid.NewGuid():N}", gone)));
        await Assert.ThrowsAsync<NotFoundException>(() => w.Admin.Send(new CreateBranchCommand($"branch-{Guid.NewGuid():N}", 999_999_999)));
        await Assert.ThrowsAsync<NotFoundException>(() => w.SwitchWarehouse(w.Branch, gone));
        await Assert.ThrowsAsync<NotFoundException>(() => w.SwitchWarehouse(w.Branch, 999_999_999));

        Assert.Equal(before, await w.WarehouseOf(w.Branch));
    }

    [Fact]
    public async Task A_new_branch_gets_its_own_warehouse_unless_it_is_given_one()
    {
        var w = await db.NewWorld();
        var shared = await w.WarehouseOf(w.Branch);
        var count = (await w.Admin.Send(new GetAllWarehouseQuery())).Count;

        var sharing = await w.NewBranch(shared);

        Assert.Equal(shared, await w.WarehouseOf(sharing));
        Assert.Equal(count, (await w.Admin.Send(new GetAllWarehouseQuery())).Count);

        var ownBranch = await w.NewBranch();
        var own = (await w.Admin.Send(new GetAllBranchesQuery())).Single(branch => branch.Id == ownBranch);
        Assert.NotEqual(shared, own.WarehouseId);
        Assert.Equal(own.Name, own.WarehouseName);
        Assert.Equal(count + 1, (await w.Admin.Send(new GetAllWarehouseQuery())).Count);
    }

    [Fact]
    public async Task Updating_a_branch_without_a_warehouse_keeps_the_current_one()
    {
        var w = await db.NewWorld();
        var warehouse = await w.WarehouseOf(w.Branch);
        var branch = (await w.Admin.Send(new GetAllBranchesQuery())).Single(b => b.Id == w.Branch);

        await w.Admin.Send(new UpdateBranchCommand(w.Branch, $"{branch.Name}-renamed", false));

        var updated = (await w.Admin.Send(new GetAllBranchesQuery())).Single(b => b.Id == w.Branch);
        Assert.Equal(warehouse, updated.WarehouseId);
        Assert.False(updated.IsActive);
    }
}
