namespace VoltStream.Application.Tests.Ledger;

using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Features.CustomerOperations.Commands;
using VoltStream.Application.Features.Payments.Commands;
using VoltStream.Application.Tests.Infrastructure;

[Collection(DatabaseCollection.Name)]
public sealed class LedgerInvariantTests(PostgresFixture db)
{
    private async Task<(World World, long Product)> Arrange()
    {
        var world = await db.NewWorld();
        var product = await world.NewProduct();
        await world.Supply(product, rolls: 20, lengthPerRoll: 100, price: 5_000);

        return (world, product);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(250_000)]
    [InlineData(-400_000)]
    public async Task New_customer_shows_the_opening_balance_everywhere(int opening)
    {
        var w = await db.NewWorld();
        var customer = await w.NewCustomer((w.Uzs, opening));

        await LedgerAssert.Balances(w, customer, (w.Uzs, opening));
    }

    [Fact]
    public async Task Sale_creation_charges_the_customer()
    {
        var (w, product) = await Arrange();
        var customer = await w.NewCustomer((w.Uzs, -100_000));

        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_100_000));
    }

    [Fact]
    public async Task Sale_with_saved_discount_charges_the_full_price_and_keeps_the_discount()
    {
        var (w, product) = await Arrange();
        var customer = await w.NewCustomer();

        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 10_000, DiscountRate: 5)]));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -2_000_000));
        await LedgerAssert.Discount(w, customer, 100_000);
    }

    [Fact]
    public async Task Sale_with_applied_discount_charges_the_net_price()
    {
        var (w, product) = await Arrange();
        var customer = await w.NewCustomer();

        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 10_000, DiscountRate: 5)], discountApplied: true));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_900_000));
        await LedgerAssert.Discount(w, customer, 0);
    }

    [Fact]
    public async Task Sale_edit_replaces_the_charge()
    {
        var (w, product) = await Arrange();
        var customer = await w.NewCustomer((w.Uzs, 300_000));
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));

        await w.Admin.Send(World.Edit(saleId, w.Sale(customer, w.Uzs, [new Line(product, 3, 100, 4_000)])));

        await LedgerAssert.Balances(w, customer, (w.Uzs, 300_000 - 1_200_000));
        Assert.Single((await w.Report(customer)).Operations);
    }

    [Fact]
    public async Task Sale_edit_to_another_currency_moves_the_charge_between_accounts()
    {
        var (w, product) = await Arrange();
        var customer = await w.NewCustomer((w.Uzs, 0), (w.Usd, 0));
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_000_000), (w.Usd, 0));

        await w.Admin.Send(World.Edit(saleId, w.Sale(customer, w.Usd, [new Line(product, 2, 100, 10)])));

        await LedgerAssert.Balances(w, customer, (w.Uzs, 0), (w.Usd, -2_000));
    }

    [Fact]
    public async Task Sale_edit_to_another_customer_moves_the_charge()
    {
        var (w, product) = await Arrange();
        var first = await w.NewCustomer((w.Uzs, 500_000));
        var second = await w.NewCustomer((w.Uzs, 0));
        var saleId = await w.Admin.Send(w.Sale(first, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        await LedgerAssert.Balances(w, first, (w.Uzs, -500_000));

        await w.Admin.Send(World.Edit(saleId, w.Sale(second, w.Uzs, [new Line(product, 2, 100, 5_000)])));

        await LedgerAssert.Balances(w, first, (w.Uzs, 500_000));
        await LedgerAssert.Balances(w, second, (w.Uzs, -1_000_000));
    }

    [Fact]
    public async Task Sale_deletion_restores_the_balance()
    {
        var (w, product) = await Arrange();
        var customer = await w.NewCustomer((w.Uzs, -250_000));
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));

        await w.DeleteSale(saleId);

        await LedgerAssert.Balances(w, customer, (w.Uzs, -250_000));
        Assert.Empty((await w.Report(customer)).Operations);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Payment_creation_edit_and_deletion_follow_the_balance(bool cash)
    {
        var w = await db.NewWorld();
        var currency = await w.NewCurrency(cash);
        var customer = await w.NewCustomer((currency, -800_000));

        var paymentId = await w.Admin.Send(World.Payment(customer, currency, 300_000));
        await LedgerAssert.Balances(w, customer, (currency, -500_000));

        await w.Admin.Send(World.EditPayment(paymentId, World.Payment(customer, currency, 450_000)));
        await LedgerAssert.Balances(w, customer, (currency, -350_000));

        await w.DeletePayment((await w.LastPayment(customer)).Id);
        await LedgerAssert.Balances(w, customer, (currency, -800_000));
    }

    [Fact]
    public async Task Operation_in_a_currency_without_an_account_opens_one()
    {
        var (w, product) = await Arrange();
        var customer = await w.NewCustomer((w.Uzs, 0));

        await w.Admin.Send(World.Payment(customer, w.Usd, 150, rate: 12_500));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0), (w.Usd, 150));

        var euro = await w.NewCurrency();
        await w.Admin.Send(w.Sale(customer, euro, [new Line(product, 1, 100, 4)]));
        await LedgerAssert.Balances(w, customer, (w.Uzs, 0), (w.Usd, 150), (euro, -400));
    }

    [Fact]
    public async Task Saved_discount_applied_to_the_balance_reduces_the_debt()
    {
        var (w, product) = await Arrange();
        var customer = await w.NewCustomer();
        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 10_000, DiscountRate: 5)]));

        var operation = await w.Admin.Send(new ApplyDiscountCommand(DateTimeOffset.UtcNow, customer, 60_000, false, string.Empty));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_940_000));
        await LedgerAssert.Discount(w, customer, 40_000);

        await w.Admin.Send(new DeleteCustomerOperationCommand(operation));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -2_000_000));
        await LedgerAssert.Discount(w, customer, 100_000);
    }

    [Fact]
    public async Task Saved_discount_paid_out_in_cash_leaves_the_balance_alone()
    {
        var (w, product) = await Arrange();
        var customer = await w.NewCustomer();
        await w.Admin.Send(World.Payment(customer, w.Uzs, 500_000));
        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 10_000, DiscountRate: 5)]));

        var operation = await w.Admin.Send(new ApplyDiscountCommand(DateTimeOffset.UtcNow, customer, 60_000, true, string.Empty));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_500_000));
        await LedgerAssert.Discount(w, customer, 40_000);

        await w.Admin.Send(new DeleteCustomerOperationCommand(operation));

        await LedgerAssert.Consistent(w, customer);
        await LedgerAssert.Discount(w, customer, 100_000);
    }

    [Fact]
    public async Task Multi_currency_customer_keeps_every_account_apart()
    {
        var (w, product) = await Arrange();
        var customer = await w.NewCustomer((w.Uzs, -500_000), (w.Usd, 100));

        var uzsSale = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_500_000), (w.Usd, 100));

        var usdSale = await w.Admin.Send(w.Sale(customer, w.Usd, [new Line(product, 1, 100, 3)]));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_500_000), (w.Usd, -200));

        var usdPayment = await w.Admin.Send(World.Payment(customer, w.Usd, 150, rate: 12_500));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_500_000), (w.Usd, -50));

        await w.Admin.Send(World.Payment(customer, w.Uzs, 700_000));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -800_000), (w.Usd, -50));

        await w.Admin.Send(World.Edit(usdSale, w.Sale(customer, w.Usd, [new Line(product, 2, 100, 3)])));
        await LedgerAssert.Balances(w, customer, (w.Uzs, -800_000), (w.Usd, -350));

        await w.DeleteSale(uzsSale);
        await LedgerAssert.Balances(w, customer, (w.Uzs, 200_000), (w.Usd, -350));

        await w.DeletePayment(usdPayment);
        await LedgerAssert.Balances(w, customer, (w.Uzs, 200_000), (w.Usd, -500));
    }

    [Fact]
    public async Task Operations_of_one_customer_leave_another_untouched()
    {
        var (w, product) = await Arrange();
        var first = await w.NewCustomer((w.Uzs, 100_000));
        var second = await w.NewCustomer((w.Uzs, 200_000));

        await w.Admin.Send(w.Sale(first, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        await w.Admin.Send(World.Payment(first, w.Uzs, 300_000));

        await LedgerAssert.Balances(w, first, (w.Uzs, -600_000));
        await LedgerAssert.Balances(w, second, (w.Uzs, 200_000));
    }

    [Fact]
    public async Task Failed_sale_edit_changes_nothing()
    {
        var (w, product) = await Arrange();
        var unstocked = await w.NewProduct();
        var customer = await w.NewCustomer();
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        var stockBefore = await w.Stock(product);

        await Assert.ThrowsAsync<NotFoundException>(()
            => w.Admin.Send(World.Edit(saleId, w.Sale(customer, w.Uzs, [new Line(unstocked, 1, 100, 5_000)]))));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_000_000));
        Assert.Equal(stockBefore, await w.Stock(product));
        Assert.Equal(saleId, (await w.LastSale(customer)).Id);
    }

    [Fact]
    public async Task Failed_payment_edit_changes_nothing()
    {
        var w = await db.NewWorld();
        var customer = await w.NewCustomer();
        var paymentId = await w.Admin.Send(World.Payment(customer, w.Uzs, 300_000));

        await Assert.ThrowsAsync<NotFoundException>(()
            => w.Admin.Send(World.EditPayment(paymentId, World.Payment(customer, 999_999_999, 450_000))));

        await LedgerAssert.Balances(w, customer, (w.Uzs, 300_000));
        Assert.Equal(paymentId, (await w.LastPayment(customer)).Id);
    }

    [Fact]
    public async Task Concurrent_sales_keep_all_readings_equal()
    {
        var w = await db.NewWorld();
        var customer = await w.NewCustomer();
        var products = new List<long>();

        for (var i = 0; i < 12; i++)
        {
            products.Add(await w.NewProduct());
            await w.Supply(products[i], rolls: 1, lengthPerRoll: 100, price: 1_000);
        }

        await Task.WhenAll(products.Select(product
            => w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 1_000)]))));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -12 * 100_000));
    }
}
