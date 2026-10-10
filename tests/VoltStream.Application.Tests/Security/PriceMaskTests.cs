namespace VoltStream.Application.Tests.Security;

using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.CustomerOperations.Queries;
using VoltStream.Application.Features.Customers.Queries;
using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Application.Features.Sales.Queries;
using VoltStream.Application.Features.WarehouseStocks.Queries;
using VoltStream.Application.Tests.Infrastructure;
using VoltStream.Application.Tests.Ledger;

[Collection(DatabaseCollection.Name)]
public sealed class PriceMaskTests(PostgresFixture db)
{
    private async Task<(World World, long Product, long Customer)> Arrange(decimal opening = 0)
    {
        var world = await db.NewWorld();
        var product = await world.NewProduct();
        await world.Supply(product, 10, 100, price: 5_000, discountRate: 10);

        return (world, product, await world.NewCustomer((world.Uzs, opening)));
    }

    private static long[] InOrder(IEnumerable<SaleDto> sales) => [.. sales.Select(sale => sale.Id)];

    [Fact]
    public async Task Priceless_sale_is_priced_from_the_stock_row_whatever_the_client_sends()
    {
        var (w, product, customer) = await Arrange();

        var saleId = await w.Seller.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, Price: 1)]));

        var sale = await w.Admin.Send(new GetSaleByIdQuery(saleId));
        Assert.Equal(1_000_000, sale.Amount);
        Assert.Equal(100_000, sale.Discount);
        var item = sale.Items.Single();
        Assert.Equal(5_000, item.UnitPrice);
        Assert.Equal(10, item.DiscountRate);
        await LedgerAssert.Balances(w, customer, (w.Uzs, -1_000_000));
        await LedgerAssert.Discount(w, customer, 100_000);
    }

    [Fact]
    public async Task Priceless_sale_with_applied_discount_charges_the_net_amount()
    {
        var (w, product, customer) = await Arrange();

        await w.Seller.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100)], discountApplied: true));

        await LedgerAssert.Balances(w, customer, (w.Uzs, -900_000));
        await LedgerAssert.Discount(w, customer, 0);
    }

    [Fact]
    public async Task Prices_sent_by_a_priced_user_are_kept_and_remembered_on_the_stock_row()
    {
        var (w, product, customer) = await Arrange();

        var first = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100, 7_000, DiscountRate: 5)]));
        Assert.Equal(700_000, (await w.Admin.Send(new GetSaleByIdQuery(first))).Amount);

        var next = await w.Seller.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100)]));
        var sale = await w.Admin.Send(new GetSaleByIdQuery(next));
        Assert.Equal(700_000, sale.Amount);
        Assert.Equal(35_000, sale.Discount);
    }

    [Fact]
    public async Task A_sale_shows_no_money_to_a_user_without_price_permission()
    {
        var (w, product, customer) = await Arrange();
        var saleId = await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000, DiscountRate: 10)]));

        var masked = await w.Seller.Send(new GetSaleByIdQuery(saleId));

        Assert.Equal(0, masked.Amount);
        Assert.Equal(0, masked.Discount);
        Assert.Equal(0, masked.CustomerOperation.Amount);
        Assert.Equal(string.Empty, masked.CustomerOperation.Description);
        var item = masked.Items.Single();
        Assert.Equal(new[] { 0m, 0m, 0m, 0m }, new[] { item.UnitPrice, item.DiscountRate, item.TotalAmount, item.FinalAmount });
        Assert.Equal(200, masked.Length);

        Assert.Equal(1_000_000, (await w.Cashier.Send(new GetSaleByIdQuery(saleId))).Amount);
    }

    [Fact]
    public async Task Price_filters_and_sorting_do_not_work_without_price_permission()
    {
        var (w, product, customer) = await Arrange();
        var big = await w.Seller.Send(w.Sale(customer, w.Uzs, [new Line(product, 3, 100)]));
        var small = await w.Seller.Send(w.Sale(customer, w.Uzs, [new Line(product, 1, 100)]));

        SaleFilterQuery Filtered() => new() { Filters = new() { ["customerId"] = [customer.ToString()], ["amount"] = [">1000000"] } };
        Assert.Equal([big], InOrder(await w.Admin.Send(Filtered())));
        Assert.Equal([big, small], InOrder(await w.Seller.Send(Filtered())));

        SaleFilterQuery Sorted() => new() { SortBy = "Amount", Filters = new() { ["customerId"] = [customer.ToString()] } };
        Assert.Equal([small, big], InOrder(await w.Admin.Send(Sorted())));
        Assert.Equal([big, small], InOrder(await w.Seller.Send(Sorted())));
    }

    [Fact]
    public async Task Stock_prices_are_hidden_from_a_user_without_price_permission()
    {
        var (w, product, _) = await Arrange();

        var masked = await w.Seller.Send(new GetAllWarehouseStockQuery(w.Branch));

        Assert.NotEmpty(masked);
        Assert.All(masked, row => Assert.Equal(new[] { 0m, 0m }, new[] { row.UnitPrice, row.DiscountRate }));

        var shown = (await w.Admin.Send(new GetAllWarehouseStockQuery(w.Branch))).Single(row => row.Product.Id == product);
        Assert.Equal(new[] { 5_000m, 10m }, new[] { shown.UnitPrice, shown.DiscountRate });
    }

    [Fact]
    public async Task Customer_accounts_are_hidden_from_a_user_without_report_permission()
    {
        var (w, _, customer) = await Arrange(opening: 250_000);

        Assert.Null((await w.Seller.Send(new GetCustomerByIdQuery(customer))).Accounts);
        Assert.Null((await w.Seller.Send(new GetAllCustomersQuery())).Single(c => c.Id == customer).Accounts);

        Assert.Equal(250_000, (await w.Cashier.Send(new GetCustomerByIdQuery(customer))).Accounts.Single().Balance);
    }

    [Fact]
    public async Task Customer_report_shows_no_money_to_a_user_without_price_permission()
    {
        var (w, product, customer) = await Arrange(opening: 250_000);
        await w.Admin.Send(w.Sale(customer, w.Uzs, [new Line(product, 2, 100, 5_000)]));
        var reader = w.As(TestUser.With(w.Branch, Permissions.ReportsView));
        var query = new GetCustomerOperationByCustomerIdQuery(customer, null, null, null);

        var masked = await reader.Send(query);

        Assert.Equal(new[] { 0m, 0m }, new[] { masked.BeginBalance, masked.EndBalance });
        Assert.NotEmpty(masked.Operations);
        Assert.All(masked.Operations, operation =>
        {
            Assert.Equal(0, operation.Amount);
            Assert.Equal(string.Empty, operation.Description);
        });

        Assert.Equal(-750_000, (await w.Cashier.Send(query)).EndBalance);
    }

    [Fact]
    public async Task Debt_report_needs_report_permission()
    {
        var (w, _, customer) = await Arrange();

        await Assert.ThrowsAsync<ForbiddenException>(() => w.Seller.Send(new CustomerBalanceQuery { CustomerId = customer }));
        await Assert.ThrowsAsync<ForbiddenException>(() => w.Seller.Send(new CustomerBalanceSummaryQuery()));

        Assert.Single(await w.Cashier.Send(new CustomerBalanceQuery { CustomerId = customer }));
    }
}
