namespace VoltStream.Application.Tests.Receipts;

using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Application.Features.Sales.Queries;
using VoltStream.Application.Tests.Infrastructure;

[Collection(DatabaseCollection.Name)]
public sealed class ReceiptCompositionTests(PostgresFixture db)
{
    private async Task<(World World, long Product, long Customer)> Arrange()
    {
        var world = await db.NewWorld();
        var product = await world.NewProduct();

        foreach (var length in new decimal[] { 30, 50, 100, 400 })
            await world.Supply(product, rolls: 20, lengthPerRoll: length, price: 1_000);

        return (world, product, await world.NewCustomer());
    }

    private static async Task<SaleReceiptDto> Receipt(World w, long customer, params Line[] lines)
    {
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, lines));

        return await w.Admin.Send(new GetSaleReceiptQuery(saleId));
    }

    [Fact]
    public async Task Whole_rolls_read_as_count_times_length()
    {
        var (w, product, customer) = await Arrange();

        var item = (await Receipt(w, customer, new Line(product, 3, 100, 1_000))).Items.Single();

        Assert.Equal("3 × 100", item.Composition);
        Assert.Equal([new RollGroupDto(100, 3)], item.Rolls);
        Assert.Equal(100, item.LengthPerRoll);
        Assert.Equal(3, item.RollCount);
        Assert.Equal(300, item.TotalLength);
    }

    [Fact]
    public async Task Cut_roll_reads_as_full_rolls_plus_the_cut_piece()
    {
        var (w, product, customer) = await Arrange();

        var item = (await Receipt(w, customer, new Line(product, 6, 400, 1_000, Length: 2018))).Items.Single();

        Assert.Equal("5 × 400 + 1 × 18", item.Composition);
        Assert.Equal([new RollGroupDto(400, 5), new RollGroupDto(18, 1)], item.Rolls);
    }

    [Fact]
    public async Task Rows_keep_the_order_they_were_entered_and_equal_lengths_merge()
    {
        var (w, product, customer) = await Arrange();

        var receipt = await Receipt(w, customer,
            new Line(product, 2, 50, 1_000),
            new Line(product, 1, 100, 1_000, Length: 70),
            new Line(product, 1, 100, 1_000),
            new Line(product, 1, 50, 1_000, Length: 30));

        var item = receipt.Items.Single();
        Assert.Equal("2 × 50 + 1 × 70 + 1 × 100 + 1 × 30", item.Composition);
        Assert.Equal(0, item.LengthPerRoll);
        Assert.Equal(5, item.RollCount);
        Assert.Equal(100 + 70 + 100 + 30, item.TotalLength);
    }

    [Fact]
    public async Task Entering_the_rows_the_other_way_round_reverses_the_composition()
    {
        var (w, product, customer) = await Arrange();

        var receipt = await Receipt(w, customer,
            new Line(product, 1, 50, 1_000, Length: 30),
            new Line(product, 1, 100, 1_000),
            new Line(product, 1, 100, 1_000, Length: 70),
            new Line(product, 2, 50, 1_000));

        Assert.Equal("1 × 30 + 1 × 100 + 1 × 70 + 2 × 50", receipt.Items.Single().Composition);
    }

    [Fact]
    public async Task Rows_of_equal_length_merge_at_their_first_position()
    {
        var (w, product, customer) = await Arrange();

        var receipt = await Receipt(w, customer,
            new Line(product, 1, 50, 1_000),
            new Line(product, 1, 100, 1_000),
            new Line(product, 1, 50, 1_000));

        Assert.Equal("2 × 50 + 1 × 100", receipt.Items.Single().Composition);
    }

    [Fact]
    public async Task Each_product_gets_its_own_item_in_the_order_it_first_appears()
    {
        var (w, first, customer) = await Arrange();
        var second = await w.NewProduct();
        await w.Supply(second, rolls: 20, lengthPerRoll: 100, price: 1_000);

        var receipt = await Receipt(w, customer,
            new Line(second, 1, 100, 1_000),
            new Line(first, 1, 100, 1_000),
            new Line(second, 2, 100, 1_000));

        Assert.Equal(["3 × 100", "1 × 100"], receipt.Items.Select(item => item.Composition));
        Assert.Equal([3, 1], receipt.Items.Select(item => item.RollCount));
    }

    [Fact]
    public async Task Prices_show_only_to_users_who_may_see_them()
    {
        var (w, product, customer) = await Arrange();
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs,
            [new Line(product, 1, 100, 1_000), new Line(product, 1, 50, 2_000)]));

        var priced = await w.Admin.Send(new GetSaleReceiptQuery(saleId));
        Assert.True(priced.HasPrices);
        Assert.Equal(100 * 1_000 + 50 * 2_000, priced.Amount);
        Assert.Null(priced.Items.Single().UnitPrice);
        Assert.Equal(200_000, priced.Items.Single().TotalAmount);

        var own = await w.Seller.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100)]));
        var masked = await w.Seller.Send(new GetSaleReceiptQuery(own));
        Assert.False(masked.HasPrices);
        Assert.Null(masked.Amount);
        Assert.Null(masked.Discount);
        Assert.Null(masked.Items.Single().UnitPrice);
        Assert.Null(masked.Items.Single().TotalAmount);
    }

    [Fact]
    public async Task One_price_across_the_rows_shows_as_the_unit_price()
    {
        var (w, product, customer) = await Arrange();

        var item = (await Receipt(w, customer,
            new Line(product, 1, 100, 1_000),
            new Line(product, 1, 100, 1_000, Length: 60))).Items.Single();

        Assert.Equal(1_000, item.UnitPrice);
        Assert.Equal(100, item.LengthPerRoll);
    }

    [Fact]
    public async Task Weight_follows_the_sold_length()
    {
        var (w, _, customer) = await Arrange();
        var weighed = await w.NewProduct(weightTracked: true, weightPerUnit: 0.25m);
        await w.Supply(weighed, rolls: 5, lengthPerRoll: 100, price: 1_000);

        var receipt = await Receipt(w, customer, new Line(weighed, 2, 100, 1_000, Length: 150));

        Assert.Equal(150 * 0.25m, receipt.TotalWeight);
        Assert.Equal(0.25m, receipt.Items.Single().WeightPerUnit);
    }

    [Fact]
    public async Task Sale_analysis_shows_the_composition_of_each_row_separately()
    {
        var (w, product, customer) = await Arrange();
        await Receipt(w, customer,
            new Line(product, 2, 50, 1_000),
            new Line(product, 1, 100, 1_000, Length: 70));

        var today = DateTimeOffset.UtcNow;
        var history = await w.Admin.Send(new SaleItemHistoryQuery
        {
            BeginDate = today.AddDays(-1),
            EndDate = today.AddDays(1),
            CustomerId = customer
        });

        Assert.Equal(["1 × 70", "2 × 50"], history.Select(row => row.Composition));
    }
}
