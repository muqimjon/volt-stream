namespace VoltStream.Application.Tests.Stock;

using System.Net;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Features.Supplies.Commands;
using VoltStream.Application.Tests.Infrastructure;

[Collection(DatabaseCollection.Name)]
public sealed class SupplyPriceTests(PostgresFixture db)
{
    private async Task<(World World, long Product, long Supply)> Arrange()
    {
        var world = await db.NewWorld();
        var product = await world.NewProduct();

        return (world, product, await world.Supply(product, 5, 100, 5_000, 10));
    }

    private static Task<(int Rolls, decimal Price, decimal Rate)> Row(World w, long product)
        => w.Db.Read(async context =>
        {
            var row = await context.WarehouseStocks
                .Where(stock => stock.ProductId == product)
                .Select(stock => new { stock.RollCount, stock.UnitPrice, stock.DiscountRate })
                .SingleAsync();

            return (row.RollCount, row.UnitPrice, row.DiscountRate);
        });

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(5_000, -1)]
    [InlineData(5_000, 150)]
    public async Task A_supply_with_a_negative_price_or_a_rate_outside_zero_and_hundred_is_rejected(decimal price, decimal rate)
    {
        var (w, product, _) = await Arrange();

        await Rejection.With(HttpStatusCode.BadRequest, () => w.Supply(product, 2, 100, price, rate));

        Assert.Equal((5, 5_000m, 10m), await Row(w, product));
    }

    [Fact]
    public async Task An_edit_with_a_negative_price_or_a_rate_over_hundred_is_rejected()
    {
        var (w, product, supply) = await Arrange();

        await Rejection.With(HttpStatusCode.BadRequest, () => w.Admin.Send(w.EditSupply(supply, product, 5, 100, -1)));
        await Rejection.With(HttpStatusCode.BadRequest, () => w.Admin.Send(w.EditSupply(supply, product, 5, 100, 5_000) with { DiscountRate = 150 }));

        Assert.Equal((5, 5_000m, 10m), await Row(w, product));
    }

    [Fact]
    public async Task A_priceless_supply_still_keeps_the_stock_price()
    {
        var (w, product, _) = await Arrange();

        await w.Seller.Send(new CreateSupplyCommand(
            DateTimeOffset.UtcNow, w.CategoryId, string.Empty, product, string.Empty, 2, "m", 100, 200, null, null, w.Branch));

        Assert.Equal((7, 5_000m, 10m), await Row(w, product));
    }
}
