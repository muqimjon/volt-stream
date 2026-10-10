namespace VoltStream.Application.Tests.Ledger;

using System.Net;
using VoltStream.Application.Features.Sales.Commands;
using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Application.Features.Sales.Queries;
using VoltStream.Application.Tests.Infrastructure;

[Collection(DatabaseCollection.Name)]
public sealed class SaleTotalsTests(PostgresFixture db)
{
    private async Task<(World World, long Product, long Customer)> Arrange()
    {
        var world = await db.NewWorld();
        var product = await world.NewProduct();
        await world.Supply(product, 10, 100, 5_000);

        return (world, product, await world.NewCustomer());
    }

    // Klient arifmetikasi xato bo'lsa ham server o'zi hisoblashini ko'rsatish uchun hamma summalar buziladi
    private static CreateSaleCommand Tampered(CreateSaleCommand sale)
        => sale with
        {
            Amount = 1,
            Discount = 7,
            Length = 1,
            RollCount = 99,
            Items = [.. sale.Items.Select(item => item with { TotalAmount = 3, FinalAmount = 2 })]
        };

    private static void Saved(SaleDto sale, decimal amount, decimal discount, decimal length, int rolls)
    {
        Assert.Equal(amount, sale.Amount);
        Assert.Equal(discount, sale.Discount);
        Assert.Equal(length, sale.Length);
        Assert.Equal(rolls, sale.RollCount);
        Assert.All(sale.Items, item =>
        {
            Assert.Equal(item.TotalLength * item.UnitPrice, item.TotalAmount);
            Assert.Equal(item.TotalAmount - item.DiscountAmount, item.FinalAmount);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task The_ledger_takes_server_totals_whatever_the_client_adds_up(bool discountApplied)
    {
        var (w, product, customer) = await Arrange();
        var sale = w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000, DiscountRate: 10), new Line(product, 1, 100, 4_000)], discountApplied);

        var saleId = await w.Admin.Send(Tampered(sale));

        var amount = discountApplied ? 1_300_000 : 1_400_000;
        Saved(await w.Admin.Send(new GetSaleByIdQuery(saleId)), amount, 100_000, 300, 3);
        await LedgerAssert.Balances(w, customer, (w.Uzs, -amount));
        await LedgerAssert.Discount(w, customer, discountApplied ? 0 : 100_000);
    }

    [Fact]
    public async Task A_discount_entered_as_an_amount_is_kept_as_entered()
    {
        var (w, product, customer) = await Arrange();
        var sale = w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)], discountApplied: true);
        sale.Items[0].DiscountAmount = 33_333;
        sale.Items[0].DiscountRate = 3.33m;

        var saleId = await w.Admin.Send(Tampered(sale));

        var saved = await w.Admin.Send(new GetSaleByIdQuery(saleId));
        Saved(saved, 966_667, 33_333, 200, 2);
        Assert.Equal(33_333, saved.Items.Single().DiscountAmount);
        await LedgerAssert.Balances(w, customer, (w.Uzs, -966_667));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public async Task A_discount_outside_zero_and_the_line_total_is_rejected(decimal discount)
    {
        var (w, product, customer) = await Arrange();
        var sale = w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]);
        sale.Items[0].DiscountAmount = discount;

        await Rejection.With(HttpStatusCode.BadRequest, () => w.Admin.Send(sale));

        Assert.Empty(await w.SalesOf(customer));
        Assert.Equal([new StockRow(100, 10, 1_000)], await w.Stock(product));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0));
    }

    [Theory]
    [InlineData(0, 100, 100)]
    [InlineData(2, 0, 100)]
    [InlineData(2, 100, 0)]
    [InlineData(2, 100, 100)]
    [InlineData(2, 100, 201)]
    [InlineData(3, 100, 150)]
    public async Task Quantities_that_do_not_fit_the_rolls_are_rejected(int rolls, decimal lengthPerRoll, decimal length)
    {
        var (w, product, customer) = await Arrange();

        await Rejection.With(HttpStatusCode.BadRequest,
            () => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, rolls, lengthPerRoll, 5_000, Length: length)])));

        Assert.Empty(await w.SalesOf(customer));
        Assert.Equal([new StockRow(100, 10, 1_000)], await w.Stock(product));
    }

    [Theory]
    [InlineData(2, 101)]
    [InlineData(2, 200)]
    public async Task Quantities_on_the_roll_boundaries_are_accepted(int rolls, decimal length)
    {
        var (w, product, customer) = await Arrange();

        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, rolls, 100, 5_000, Length: length)]));

        Saved(await w.Admin.Send(new GetSaleByIdQuery(saleId)), length * 5_000, 0, length, rolls);
    }

    [Fact]
    public async Task Editing_a_sale_recalculates_the_totals_too()
    {
        var (w, product, customer) = await Arrange();
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 5_000)]));

        await w.Admin.Send(World.Edit(saleId, Tampered(w.Sale(customer, w.Uzs, [new Line(product, 3, 100, 6_000, DiscountRate: 5)], discountApplied: true))));

        Saved(await w.Admin.Send(new GetSaleByIdQuery((await w.LastSale(customer)).Id)), 1_710_000, 90_000, 300, 3);
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_710_000));
        Assert.Equal([new StockRow(100, 7, 700)], await w.Stock(product));
    }

    [Fact]
    public async Task Confirming_a_list_recalculates_the_totals_too()
    {
        var (w, product, customer) = await Arrange();
        var draft = await w.Seller.Send(w.Draft([new Line(product, 2, 100)], customer));

        var saleId = await w.Manager.Send(Tampered(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)])) with { SaleDraftId = draft });

        Saved(await w.Admin.Send(new GetSaleByIdQuery(saleId)), 1_000_000, 0, 200, 2);
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_000_000));
    }

    [Fact]
    public async Task A_list_line_that_does_not_fit_its_rolls_is_rejected()
    {
        var (w, product, customer) = await Arrange();

        await Rejection.With(HttpStatusCode.BadRequest,
            () => w.Seller.Send(w.Draft([new Line(product, 3, 100, Length: 150)], customer)));
    }
}
