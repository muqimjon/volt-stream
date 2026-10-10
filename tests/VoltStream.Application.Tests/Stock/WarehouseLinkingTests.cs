namespace VoltStream.Application.Tests.Stock;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Features.Warehouses.Commands;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Application.Tests.Ledger;
using VoltStream.Domain.Entities;
using VoltStream.Infrastructure.Persistence;

// Initializer butun jadvallar bo'yicha ishlaydi, shuning uchun har test o'z toza bazasida
[Collection(DatabaseCollection.Name)]
public sealed class WarehouseLinkingTests(PostgresFixture db)
{
    private static Task Link(TestDatabase database)
        => database.Write(context => DatabaseInitializer.LinkBranchWarehousesAsync(context, CancellationToken.None));

    private static Task<string> Links(TestDatabase database)
        => database.Read(async context =>
        {
            var branches = await context.Branches.OrderBy(b => b.Id).Select(b => new { b.Id, b.WarehouseId }).ToListAsync();
            var sales = await context.Sales.OrderBy(s => s.Id).Select(s => new { s.Id, s.WarehouseId }).ToListAsync();
            var supplies = await context.Supplies.OrderBy(s => s.Id).Select(s => new { s.Id, s.WarehouseId }).ToListAsync();
            var warehouses = await context.Warehouses.CountAsync();

            return $"branches {string.Join(',', branches.Select(b => $"{b.Id}>{b.WarehouseId}"))}; "
                + $"sales {string.Join(',', sales.Select(s => $"{s.Id}>{s.WarehouseId}"))}; "
                + $"supplies {string.Join(',', supplies.Select(s => $"{s.Id}>{s.WarehouseId}"))}; warehouses {warehouses}";
        });

    [Fact]
    public async Task Linker_pairs_branches_with_free_warehouses_in_id_order_and_fills_the_documents()
    {
        await using var legacy = await db.NewDatabase();
        var legacyWarehouse = await new Actor(legacy.Services, TestUser.Admin()).Send(new CreateWarehouseCommand("legacy"));
        var w = await legacy.NewWorld();
        var product = await w.NewProduct();
        var customer = await w.NewCustomer();
        var firstSupply = await w.Supply(product, 5, 100, 5_000);
        var secondSupply = await w.Supply(product, 5, 100, 5_000, branchId: w.OtherBranch);
        var firstSale = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        var secondSale = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)], branchId: w.OtherBranch));
        var firstWarehouse = await w.WarehouseOf(w.Branch);
        var secondWarehouse = await w.WarehouseOf(w.OtherBranch);

        // 1.2 dan o'tgandagi holat: hech narsa omborga bog'lanmagan, qo'shimcha filialda esa ombor umuman yo'q
        var extraName = $"extra-{Guid.NewGuid():N}";
        await legacy.Write(async context =>
        {
            await context.Branches.ExecuteUpdateAsync(s => s.SetProperty(b => b.WarehouseId, (long?)null));
            await context.Sales.ExecuteUpdateAsync(s => s.SetProperty(x => x.WarehouseId, (long?)null));
            await context.Supplies.ExecuteUpdateAsync(s => s.SetProperty(x => x.WarehouseId, (long?)null));
            context.Branches.Add(new Branch { Name = extraName, NormalizedName = extraName.ToUpperInvariant() });
            await context.SaveChangesAsync();
        });

        await Link(legacy);

        var branches = await legacy.Read(context => context.Branches.OrderBy(b => b.Id).Select(b => new { b.Id, b.Name, b.WarehouseId }).ToListAsync());
        Assert.Equal(4, branches.Count);
        Assert.Equal(legacyWarehouse, branches[0].WarehouseId);
        Assert.Equal(firstWarehouse, branches.Single(b => b.Id == w.Branch).WarehouseId);
        Assert.Equal(secondWarehouse, branches.Single(b => b.Id == w.OtherBranch).WarehouseId);

        var extraWarehouse = await legacy.Read(context => context.Warehouses
            .Where(warehouse => warehouse.Branches.Any(b => b.Name == extraName))
            .Select(warehouse => new { warehouse.Id, warehouse.Name })
            .SingleAsync());
        Assert.Equal(extraName, extraWarehouse.Name);
        Assert.DoesNotContain(extraWarehouse.Id, new[] { legacyWarehouse, firstWarehouse, secondWarehouse });

        Assert.Equal(firstWarehouse, await w.WarehouseOfSale(firstSale));
        Assert.Equal(secondWarehouse, await w.WarehouseOfSale(secondSale));
        Assert.Equal(firstWarehouse, await w.WarehouseOfSupply(firstSupply));
        Assert.Equal(secondWarehouse, await w.WarehouseOfSupply(secondSupply));

        await w.DeleteSale(firstSale);
        Assert.Equal([new StockRow(100, 5, 500)], await w.StockIn(firstWarehouse, product));
        Assert.Equal([new StockRow(100, 4, 400)], await w.StockIn(secondWarehouse, product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -500_000));
    }

    [Fact]
    public async Task A_second_run_changes_nothing()
    {
        await using var legacy = await db.NewDatabase();
        var w = await legacy.NewWorld();
        var product = await w.NewProduct();
        await w.Supply(product, 5, 100, 5_000);
        await w.Admin.Send(w.Sale(await w.NewCustomer(), w.Uzs, [new Line(product, 2, 100, 5_000)]));
        await legacy.Write(async context =>
        {
            await context.Branches.ExecuteUpdateAsync(s => s.SetProperty(b => b.WarehouseId, (long?)null));
            await context.Sales.ExecuteUpdateAsync(s => s.SetProperty(x => x.WarehouseId, (long?)null));
            await context.Supplies.ExecuteUpdateAsync(s => s.SetProperty(x => x.WarehouseId, (long?)null));
        });
        await Link(legacy);
        var linked = await Links(legacy);

        await Link(legacy);

        Assert.Equal(linked, await Links(legacy));
    }

    [Fact]
    public async Task Linker_skips_deleted_warehouses_and_leaves_spare_free_ones_alone()
    {
        await using var legacy = await db.NewDatabase();
        var admin = new Actor(legacy.Services, TestUser.Admin());
        var deleted = await admin.Send(new CreateWarehouseCommand("deleted"));
        await admin.Send(new DeleteWarehouseCommand(deleted));
        var first = await admin.Send(new CreateWarehouseCommand("first"));
        await admin.Send(new CreateWarehouseCommand("spare"));

        await Link(legacy);

        var defaultBranch = await legacy.Read(context => context.Branches.SingleAsync());
        Assert.Equal(first, defaultBranch.WarehouseId);
        Assert.Equal([first], await legacy.Read(context => context.Warehouses.Where(warehouse => warehouse.Branches.Any()).Select(warehouse => warehouse.Id).ToListAsync()));
    }

    [Fact]
    public async Task Linker_names_a_nameless_warehouse_after_its_branch_and_keeps_named_ones()
    {
        await using var legacy = await db.NewDatabase();
        var firstName = $"extra-{Guid.NewGuid():N}";
        var secondName = $"extra-{Guid.NewGuid():N}";
        await legacy.Write(async context =>
        {
            // v1 dan ko'chgan ombor nomsiz: bo'sh va faqat bo'shliqdan iborat nom
            context.Warehouses.AddRange(
                new Warehouse { Name = string.Empty, NormalizedName = string.Empty },
                new Warehouse { Name = "Main store", NormalizedName = "MAIN STORE" },
                new Warehouse { Name = "   ", NormalizedName = "   " });
            context.Branches.AddRange(
                new Branch { Name = firstName, NormalizedName = firstName.ToUpperInvariant() },
                new Branch { Name = secondName, NormalizedName = secondName.ToUpperInvariant() });
            await context.SaveChangesAsync();
        });
        var defaultBranch = await legacy.Read(context => context.Branches.OrderBy(b => b.Id).FirstAsync());

        await Link(legacy);

        var warehouses = await legacy.Read(context => context.Warehouses
            .OrderBy(warehouse => warehouse.Id)
            .Select(warehouse => new { warehouse.Name, warehouse.NormalizedName })
            .ToListAsync());
        Assert.Equal(3, warehouses.Count);
        Assert.Equal(defaultBranch.Name, warehouses[0].Name);
        Assert.Equal(defaultBranch.NormalizedName, warehouses[0].NormalizedName);
        Assert.Equal("Main store", warehouses[1].Name);
        Assert.Equal("MAIN STORE", warehouses[1].NormalizedName);
        Assert.Equal(secondName, warehouses[2].Name);
        Assert.Equal(secondName.ToUpperInvariant(), warehouses[2].NormalizedName);
    }
}
