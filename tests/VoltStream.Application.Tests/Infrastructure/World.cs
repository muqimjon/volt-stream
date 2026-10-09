namespace VoltStream.Application.Tests.Infrastructure;

using System.Text.Json;
using VoltStream.Application.Features.Accounts.DTOs;
using VoltStream.Application.Features.Branches.Commands;
using VoltStream.Application.Features.Categories.Commands;
using VoltStream.Application.Features.Currencies.Commands;
using VoltStream.Application.Features.CustomerOperations.Commands;
using VoltStream.Application.Features.CustomerOperations.DTOs;
using VoltStream.Application.Features.CustomerOperations.Queries;
using VoltStream.Application.Features.Customers.Commands;
using VoltStream.Application.Features.Payments.Commands;
using VoltStream.Application.Features.Payments.DTOs;
using VoltStream.Application.Features.Payments.Queries;
using VoltStream.Application.Features.Products.Commands;
using VoltStream.Application.Features.Sales.Commands;
using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Application.Features.Sales.Queries;
using VoltStream.Application.Features.Supplies.Commands;
using VoltStream.Application.Features.WarehouseStocks.Queries;
using VoltStream.Domain.Enums;

public sealed record Line(
    long ProductId,
    int Rolls,
    decimal LengthPerRoll,
    decimal Price = 0,
    decimal DiscountRate = 0,
    decimal? Length = null);

public sealed record StockRow(decimal LengthPerRoll, int Rolls, decimal Length);

// Har test o'z filiallari, valyutalari va mahsulotlarini yaratadi — bitta umumiy bazada testlar bir-biriga tegmaydi
public sealed class World
{
    private readonly IServiceProvider services;
    private long category;

    private World(IServiceProvider services)
    {
        this.services = services;
        Admin = As(TestUser.Admin());
    }

    public Actor Admin { get; }
    public Actor Seller { get; private set; } = default!;
    public Actor Cashier { get; private set; } = default!;
    public Actor Manager { get; private set; } = default!;
    public Actor OtherSeller { get; private set; } = default!;
    public Actor OtherManager { get; private set; } = default!;

    public long Branch { get; private set; }
    public long OtherBranch { get; private set; }
    public long Uzs { get; private set; }
    public long Usd { get; private set; }

    public static async Task<World> CreateAsync(IServiceProvider services)
    {
        var world = new World(services);
        var tag = Unique();

        world.Branch = await world.Admin.Send(new CreateBranchCommand($"branch-{tag}"));
        world.OtherBranch = await world.Admin.Send(new CreateBranchCommand($"other-{tag}"));
        world.Uzs = await world.NewCurrency();
        world.Usd = await world.NewCurrency(rate: 12_500);
        world.category = await world.Admin.Send(new CreateCategoryCommand($"category-{tag}"));

        world.Seller = world.As(TestUser.Of(UserRole.Seller, world.Branch));
        world.Cashier = world.As(TestUser.Of(UserRole.Cashier, world.Branch));
        world.Manager = world.As(TestUser.Of(UserRole.Manager, world.Branch));
        world.OtherSeller = world.As(TestUser.Of(UserRole.Seller, world.OtherBranch));
        world.OtherManager = world.As(TestUser.Of(UserRole.Manager, world.OtherBranch));

        return world;
    }

    public Actor As(TestUser user) => new(services, user);

    public Task<long> NewCurrency(bool isCash = true, decimal rate = 1)
    {
        var tag = Unique();

        return Admin.Send(new CreateCurrencyCommand($"currency-{tag}", tag[..3].ToUpperInvariant(), tag[..1], false, true, true, 9, isCash, rate));
    }

    public Task<long> NewCustomer(params (long Currency, decimal Opening)[] accounts)
    {
        if (accounts.Length == 0)
            accounts = [(Uzs, 0)];

        return Admin.Send(new CreateCustomerCommand(
            $"customer-{Unique()}", null, null, null, null, null, null,
            [.. accounts.Select(account => Account(account.Currency, account.Opening))]));
    }

    public async Task<long> NewProduct(bool weightTracked = false, decimal? weightPerUnit = null)
        => await Admin.Send(new CreateProductCommand($"product-{Unique()}", "m", category, weightTracked, weightPerUnit));

    public Task Supply(long productId, int rolls, decimal lengthPerRoll, decimal price = 0, decimal discountRate = 0, long? branchId = null)
        => Admin.Send(new CreateSupplyCommand(
            DateTimeOffset.UtcNow, category, string.Empty, productId, string.Empty,
            rolls, "m", lengthPerRoll, rolls * lengthPerRoll, price, discountRate, branchId ?? Branch));

    public CreateSaleCommand Sale(long customerId, long currencyId, Line[] lines, bool discountApplied = false, long? branchId = null)
    {
        var items = lines.Select(line =>
        {
            var length = line.Length ?? line.Rolls * line.LengthPerRoll;
            var amount = length * line.Price;
            var discount = amount * line.DiscountRate / 100;

            return new SaleItemCommand
            {
                ProductId = line.ProductId,
                RollCount = line.Rolls,
                LengthPerRoll = line.LengthPerRoll,
                TotalLength = length,
                UnitPrice = line.Price,
                DiscountRate = line.DiscountRate,
                DiscountAmount = discount,
                TotalAmount = amount,
                FinalAmount = amount - discount
            };
        }).ToList();

        var totalDiscount = items.Sum(item => item.DiscountAmount);

        return new CreateSaleCommand(
            DateTimeOffset.UtcNow, customerId, currencyId,
            items.Sum(item => item.RollCount), items.Sum(item => item.TotalLength),
            items.Sum(item => item.TotalAmount) - (discountApplied ? totalDiscount : 0),
            discountApplied, totalDiscount, string.Empty, items, branchId ?? Branch);
    }

    public static UpdateSaleCommand Edit(long saleId, CreateSaleCommand sale)
        => new(saleId, sale.Date, sale.CustomerId, sale.CurrencyId, sale.RollCount, sale.Length,
            sale.Amount, sale.IsDiscountApplied, sale.Discount, sale.Description, sale.Items);

    public static CreatePaymentCommand Payment(long customerId, long currencyId, decimal amount, decimal rate = 1)
        => new(DateTimeOffset.UtcNow, customerId, amount, currencyId, rate, amount, string.Empty);

    public static UpdatePaymentCommand EditPayment(long paymentId, CreatePaymentCommand payment)
        => new(paymentId, payment.PaidAt, payment.CustomerId, payment.Amount, payment.CurrencyId,
            payment.ExchangeRate, payment.NetAmount, payment.Description);

    public async Task DeleteSale(long saleId)
    {
        var sale = await Admin.Send(new GetSaleByIdQuery(saleId));
        await Admin.Send(new DeleteCustomerOperationCommand(sale.CustomerOperationId));
    }

    public async Task DeletePayment(long paymentId)
    {
        var payment = await Admin.Send(new GetPaymentByIdQuery(paymentId));
        await Admin.Send(new DeleteCustomerOperationCommand(payment.CustomerOperationId));
    }

    // Tahrirlash eski yozuvni o'chirib yangisini yaratadi, shuning uchun id o'zgaradi
    public async Task<SaleDto> LastSale(long customerId)
        => (await Admin.Send(new SaleFilterQuery { Filters = new() { ["customerId"] = [customerId.ToString()] } }))
            .OrderBy(sale => sale.Id).Last();

    public async Task<PaymentDto> LastPayment(long customerId)
        => (await Admin.Send(new PaymentFilterQuery { Filters = new() { ["customerId"] = [customerId.ToString()] } }))
            .OrderBy(payment => payment.Id).Last();

    public Task<CustomerOperationSummaryDto> Report(long customerId)
        => Admin.Send(new GetCustomerOperationByCustomerIdQuery(customerId, null, null, null));

    public async Task<List<StockRow>> Stock(long productId, long? branchId = null)
        => [.. (await Admin.Send(new GetAllWarehouseStockQuery(branchId ?? Branch)))
            .Where(stock => stock.Product.Id == productId)
            .OrderBy(stock => stock.LengthPerRoll)
            .Select(stock => new StockRow(stock.LengthPerRoll, stock.RollCount, stock.TotalLength))];

    // AccountCommandDto konstruktori 1.2 (Balance bor) va 1.3 (yo'q) da farq qiladi; klientlar uni JSON bilan yuboradi, JSON ikkalasiga mos
    private static AccountCommandDto Account(long currencyId, decimal opening)
        => JsonSerializer.Deserialize<AccountCommandDto>(
            JsonSerializer.Serialize(new { openingBalance = opening, balance = opening, currencyId }),
            JsonSerializerOptions.Web)!;

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];
}
