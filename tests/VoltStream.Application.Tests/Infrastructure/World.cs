namespace VoltStream.Application.Tests.Infrastructure;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Features.Accounts.DTOs;
using VoltStream.Application.Features.Branches.Commands;
using VoltStream.Application.Features.Branches.Queries;
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
using VoltStream.Application.Features.SaleDrafts;
using VoltStream.Application.Features.SaleDrafts.Commands;
using VoltStream.Application.Features.SaleDrafts.Queries;
using VoltStream.Application.Features.Sales.Commands;
using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Application.Features.Sales.Queries;
using VoltStream.Application.Features.Settings.Commands;
using VoltStream.Application.Features.Supplies.Commands;
using VoltStream.Application.Features.Warehouses.Commands;
using VoltStream.Application.Features.WarehouseStocks.Queries;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;
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
    private readonly Dictionary<long, string> productNames = [];
    private long category;
    private string categoryName = string.Empty;

    private World(TestDatabase database)
    {
        Db = database;
        services = database.Services;
        Admin = As(TestUser.Admin());
    }

    public TestDatabase Db { get; }

    public Actor Admin { get; }
    public Actor Seller { get; private set; } = default!;
    public Actor Cashier { get; private set; } = default!;
    public Actor Manager { get; private set; } = default!;
    public Actor OtherSeller { get; private set; } = default!;
    public Actor OtherManager { get; private set; } = default!;

    public long CategoryId => category;

    public long Branch { get; private set; }
    public long OtherBranch { get; private set; }
    public long Uzs { get; private set; }
    public long Usd { get; private set; }

    public static async Task<World> CreateAsync(TestDatabase database)
    {
        var world = new World(database);
        var tag = Unique();

        world.Branch = await world.Admin.Send(new CreateBranchCommand($"branch-{tag}"));
        world.OtherBranch = await world.Admin.Send(new CreateBranchCommand($"other-{tag}"));
        world.Uzs = await world.NewCurrency();
        world.Usd = await world.NewCurrency(rate: 12_500);
        world.categoryName = $"category-{tag}";
        world.category = await world.Admin.Send(new CreateCategoryCommand(world.categoryName));

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
            [.. accounts.Select(account => new AccountCommandDto(0, account.Opening, 0, 0, account.Currency))]));
    }

    public async Task<long> NewProduct(bool weightTracked = false, decimal? weightPerUnit = null)
    {
        var name = $"product-{Unique()}";
        var id = await Admin.Send(new CreateProductCommand(name, "m", category, weightTracked, weightPerUnit));
        productNames[id] = name;

        return id;
    }

    public Task SetNegativeStock(bool allowed) => Admin.Send(new UpdateGeneralSettingsCommand(allowed));

    public CreateSaleDraftCommand Draft(Line[] lines, long? customerId = null, long? branchId = null)
        => new(DateTimeOffset.UtcNow, customerId, string.Empty,
            [.. lines.Select(line => new SaleDraftItemCommand(line.ProductId, line.LengthPerRoll, line.Rolls, line.Length ?? line.Rolls * line.LengthPerRoll))],
            branchId ?? Branch);

    public static UpdateSaleDraftCommand EditDraft(long draftId, CreateSaleDraftCommand draft)
        => new(draftId, draft.Date, draft.CustomerId, draft.Description, draft.Items);

    public async Task<long[]> OpenDraftIds(Actor actor, long? branchId = null)
        => [.. (await actor.Send(new GetOpenSaleDraftsQuery(branchId))).Select(draft => draft.Id).Order()];

    // Chop etish so'rovchisi Users jadvalidan qidiriladi, shuning uchun qatori bor foydalanuvchi
    public async Task<Actor> NewUser(UserRole role, long branchId, params string[] extraPermissions)
    {
        var permissions = RolePresets.For(role).Concat(extraPermissions).Distinct().ToArray();
        var username = $"user-{Unique()}";
        var id = await Db.Read(async context =>
        {
            var user = new User
            {
                Username = username,
                NormalizedUsername = username.ToUpperInvariant(),
                Role = role,
                IsActive = true,
                Permissions = permissions,
                BranchId = branchId
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            return user.Id;
        });

        return As(TestUser.Existing(id, branchId, permissions));
    }
    public Task<long> NewBranch(long? warehouseId = null)
        => Admin.Send(new CreateBranchCommand($"branch-{Unique()}", warehouseId));

    public Task<long> NewWarehouse()
        => Admin.Send(new CreateWarehouseCommand($"warehouse-{Unique()}"));

    public async Task<long> WarehouseOf(long branchId)
        => (await Admin.Send(new GetAllBranchesQuery())).Single(branch => branch.Id == branchId).WarehouseId!.Value;

    public async Task SwitchWarehouse(long branchId, long warehouseId)
    {
        var branch = (await Admin.Send(new GetAllBranchesQuery())).Single(b => b.Id == branchId);

        await Admin.Send(new UpdateBranchCommand(branchId, branch.Name, branch.IsActive, warehouseId));
    }

    public Task<long> Supply(long productId, int rolls, decimal lengthPerRoll, decimal price = 0, decimal discountRate = 0, long? branchId = null)
        => Admin.Send(new CreateSupplyCommand(
            DateTimeOffset.UtcNow, category, string.Empty, productId, string.Empty,
            rolls, "m", lengthPerRoll, rolls * lengthPerRoll, price, discountRate, branchId ?? Branch));

    // Tahrirlash kategoriya va mahsulotni nomi bo'yicha topadi
    public UpdateSupplyCommand EditSupply(long supplyId, long productId, int rolls, decimal lengthPerRoll, decimal price = 0)
        => new(supplyId, DateTimeOffset.UtcNow, category, categoryName, productId, productNames[productId], "m",
            rolls, lengthPerRoll, rolls * lengthPerRoll, price, 0);

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

    public async Task<List<SaleDto>> SalesOf(long customerId)
        => [.. (await Admin.Send(new SaleFilterQuery { Filters = new() { ["customerId"] = [customerId.ToString()] } })).OrderBy(sale => sale.Id)];

    // Tahrirlash eski yozuvni o'chirib yangisini yaratadi, shuning uchun id o'zgaradi
    public async Task<SaleDto> LastSale(long customerId) => (await SalesOf(customerId)).Last();

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

    // Filialga bog'lanmagan (masalan, almashtirilgandan keyingi eski) ombor qoldig'ini ko'rish uchun to'g'ridan-to'g'ri bazadan o'qiladi
    public Task<List<StockRow>> StockIn(long warehouseId, long productId)
        => Db.Read(context => context.WarehouseStocks
            .Where(stock => stock.WarehouseId == warehouseId && stock.ProductId == productId)
            .OrderBy(stock => stock.LengthPerRoll)
            .Select(stock => new StockRow(stock.LengthPerRoll, stock.RollCount, stock.TotalLength))
            .ToListAsync());

    public Task<long?> WarehouseOfSale(long saleId)
        => Db.Read(context => context.Sales.Where(sale => sale.Id == saleId).Select(sale => sale.WarehouseId).SingleAsync());

    public Task<long?> WarehouseOfSupply(long supplyId)
        => Db.Read(context => context.Supplies.Where(supply => supply.Id == supplyId).Select(supply => supply.WarehouseId).SingleAsync());

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];
}
