namespace VoltStream.Application.Tests.Stock;

using VoltStream.Application.Features.WarehouseStocks.Queries;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

[Collection(DatabaseCollection.Name)]
public sealed class StockSummaryTests(PostgresFixture db)
{
    private static async Task<List<(long Product, int Rolls, decimal Length)>> Summary(Actor actor, long[] products, long? branchId = null)
        => [.. (await actor.Send(new GetStockSummaryQuery(branchId)))
            .Where(row => products.Contains(row.ProductId))
            .Select(row => (row.ProductId, row.RollCount, row.TotalLength))];

    [Fact]
    public async Task Summary_adds_up_positive_rows_of_the_readers_warehouse()
    {
        var w = await db.NewWorld();
        var warehouse = await w.WarehouseOf(w.Branch);
        var shared = await w.NewBranch(warehouse);
        var cable = await w.NewProduct();
        var wire = await w.NewProduct();
        long[] products = [cable, wire];
        await w.SetNegativeStock(true);

        await w.Supply(cable, 5, 100, 5_000);
        await w.Supply(cable, 3, 50, 5_000, branchId: shared);
        await w.Supply(cable, 2, 100, 5_000, branchId: w.OtherBranch);
        await w.Supply(wire, 1, 100, 5_000);
        await w.Supply(wire, 2, 70, 5_000);
        await w.Admin.Send(w.Sale(await w.NewCustomer(), w.Uzs, [new Line(wire, 3, 100, 5_000)]));
        // Eski kod yaratgan manfiy bo'lak (production'da 6347): RollCount musbat, lekin uzunlik manfiy
        await w.Db.Write(context =>
        {
            context.WarehouseStocks.Add(new WarehouseStock { ProductId = cable, WarehouseId = warehouse, RollCount = 1, LengthPerRoll = -74, TotalLength = -74 });
            return context.SaveChangesAsync();
        });

        List<(long, int, decimal)> own = [(cable, 8, 650), (wire, 2, 140)];
        Assert.Equal(own, await Summary(w.Seller, products));
        Assert.Equal(own, await Summary(w.As(TestUser.Of(UserRole.Seller, shared)), products));
        Assert.Equal([(cable, 2, 200)], await Summary(w.OtherSeller, products));
        Assert.Equal([(cable, 2, 200)], await Summary(w.OtherSeller, products, w.Branch));
        Assert.Equal([(cable, 2, 200)], await Summary(w.Admin, products, w.OtherBranch));
        Assert.Equal([(cable, 10, 850), (wire, 2, 140)], await Summary(w.Admin, products));
    }
}
