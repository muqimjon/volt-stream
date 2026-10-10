namespace VoltStream.Application.Features.Sales.Commands;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json.Serialization;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.SaleDrafts;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

[RequirePermission(Permissions.SalesCreate)]
public record CreateSaleCommand(
    DateTimeOffset Date,
    long? CustomerId,
    long CurrencyId,
    int RollCount,
    decimal Length,
    decimal Amount,
    bool IsDiscountApplied,
    decimal Discount,
    string Description,
    List<SaleItemCommand> Items,
    long? BranchId = null,
    long? SaleDraftId = null)
    : IRequest<long>
{
    // Faqat UpdateSaleCommand beradi: tahrirlangan savdo asl omborida qolsin.
    // So'rov tanasidan bog'lanmaydi — klient omborni tanlay olmaydi.
    [JsonIgnore]
    public long? WarehouseId { get; init; }
}

public class CreateSaleCommandHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IMapper mapper) : IRequestHandler<CreateSaleCommand, long>
{
    public async Task<long> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        if (request.Items.Any(item => !RollComposition.IsConsistent(item.RollCount, item.LengthPerRoll, item.TotalLength)))
            throw new AppException("Savdo qatori noto'g'ri: soni va uzunligi musbat, jami (soni − 1) × uzunlikdan katta va soni × uzunlikdan oshmasin.");

        foreach (var item in request.Items)
            PriceValidation.Ensure(item.UnitPrice, item.DiscountRate);

        await context.BeginTransactionAsync(cancellationToken);

        try
        {
            var branchId = await branches.ResolveAsync(request.BranchId, cancellationToken);
            var draft = request.SaleDraftId is long draftId ? await ClaimDraftAsync(draftId, branchId, cancellationToken) : null;
            var warehouse = await GetWarehouseAsync(
                request.WarehouseId ?? await branches.WarehouseOfAsync(branchId, cancellationToken), cancellationToken);
            var customer = await GetCustomerAsync(request.CustomerId, cancellationToken);

            var descriptionBuilder = new StringBuilder();
            ApplyStockPrices(request.Items, warehouse);
            CalculateAmounts(request.Items);

            var allowNegativeStock = await context.AppSettings.AllAsync(s => s.AllowNegativeStock, cancellationToken);
            await ProcessSaleItemsAsync(request.Items, warehouse, allowNegativeStock, descriptionBuilder, cancellationToken);
            var sale = mapper.Map<Sale>(request);
            sale.BranchId = branchId;
            sale.WarehouseId = warehouse.Id;

            RecalculateTotals(sale, request.Items, request.IsDiscountApplied);

            if (customer is not null)
            {
                var account = customer.Accounts.FirstOrDefault(a => a.CurrencyId == request.CurrencyId);

                if (account is null)
                    customer.Accounts.Add(account = new()
                    {
                        CurrencyId = request.CurrencyId
                    });

                AccumulateDiscount(account, sale.Discount, request.IsDiscountApplied);
                sale.CustomerOperation = CreateCustomerOperation(sale, account, request.Description, descriptionBuilder, request.IsDiscountApplied);
            }

            if (draft is not null)
                draft.ConfirmedSale = sale;

            context.Sales.Add(sale);
            await context.CommitTransactionAsync(cancellationToken);

            return sale.Id;
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // Shartli UPDATE ro'yxat qatorini tranzaksiya oxirigacha qulflaydi: parallel ikkinchi tasdiq kutib turadi,
    // keyin holat Open emasligini ko'rib 409 oladi.
    private async Task<SaleDraft> ClaimDraftAsync(long draftId, long branchId, CancellationToken cancellationToken)
    {
        if (!user.Has(Permissions.DraftsConfirm))
            throw new ForbiddenException($"Ruxsat yetarli emas: {Permissions.DraftsConfirm}");

        var claimed = await context.SaleDrafts
            .Where(d => d.Id == draftId && d.BranchId == branchId && d.Status == SaleDraftStatus.Open)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, SaleDraftStatus.Confirmed)
                .SetProperty(d => d.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

        if (claimed == 0)
        {
            var draftBranchId = await context.SaleDrafts
                .Where(d => d.Id == draftId)
                .Select(d => (long?)d.BranchId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException(nameof(SaleDraft), nameof(SaleDraft.Id), draftId);

            if (draftBranchId != branchId)
                throw user.Has(Permissions.BranchesAll)
                    ? new ConflictException("Ro'yxat boshqa filialga tegishli.")
                    : new NotFoundException(nameof(SaleDraft), nameof(SaleDraft.Id), draftId);

            throw new ConflictException(SaleDraftExtensions.NotOpenMessage);
        }

        return await context.SaleDrafts.FirstAsync(d => d.Id == draftId, cancellationToken);
    }

    private async Task<Warehouse> GetWarehouseAsync(long warehouseId, CancellationToken cancellationToken)
    {
        return await context.Warehouses
            .Include(w => w.Stocks)
            .FirstOrDefaultAsync(w => w.Id == warehouseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse));
    }

    private void ApplyStockPrices(List<SaleItemCommand> items, Warehouse warehouse)
    {
        var canSeePrices = user.Has(Permissions.SalesViewPrices);

        foreach (var item in items)
        {
            if (canSeePrices && item.UnitPrice != 0)
                continue;

            var stock = warehouse.Stocks
                .FirstOrDefault(s => s.ProductId == item.ProductId && s.LengthPerRoll == item.LengthPerRoll);

            if (stock is null)
                continue;

            item.UnitPrice = stock.UnitPrice;
            item.DiscountRate = stock.DiscountRate;
            item.DiscountAmount = item.TotalLength * item.UnitPrice * item.DiscountRate / 100;
        }
    }

    // Qarz shu summalardan yoziladi, shuning uchun klient arifmetikasiga ishonilmaydi. Chegirma summasi esa
    // klientdan olinadi: desktopda "summa bilan chegirma" bor, foizdan qayta hisoblash foydalanuvchi niyatini buzadi.
    // Pul 2 xonaga yaxlitlanadi (klientlar ham half-up): aks holda qarzda 0,005 qoldiq qoladi.
    private static void CalculateAmounts(List<SaleItemCommand> items)
    {
        foreach (var item in items)
        {
            item.TotalAmount = Math.Round(item.TotalLength * item.UnitPrice, 2, MidpointRounding.AwayFromZero);
            item.DiscountAmount = Math.Round(item.DiscountAmount, 2, MidpointRounding.AwayFromZero);

            if (item.DiscountAmount < 0 || item.DiscountAmount > item.TotalAmount)
                throw new AppException("Chegirma summasi manfiy yoki qator summasidan katta bo'lishi mumkin emas.");

            item.FinalAmount = item.TotalAmount - item.DiscountAmount;
        }
    }

    private static void RecalculateTotals(Sale sale, List<SaleItemCommand> items, bool isDiscountApplied)
    {
        sale.Discount = items.Sum(i => i.DiscountAmount);
        sale.Amount = items.Sum(i => i.TotalAmount) - (isDiscountApplied ? sale.Discount : 0);
        sale.Length = items.Sum(i => i.TotalLength);
        sale.RollCount = items.Sum(i => i.RollCount);
    }

    private async Task<Customer?> GetCustomerAsync(long? customerId, CancellationToken cancellationToken)
    {
        if (customerId is null)
            return default;
        var customer = await context.Customers
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(a => a.Id == customerId, cancellationToken);

        return customer;
    }

    private async Task ProcessSaleItemsAsync(
        List<SaleItemCommand> saleItems,
        Warehouse warehouse,
        bool allowNegativeStock,
        StringBuilder descriptionBuilder,
        CancellationToken cancellationToken)
    {
        foreach (var item in saleItems)
        {
            var residue = warehouse.Stocks
                .FirstOrDefault(r => r.ProductId == item.ProductId && r.LengthPerRoll == item.LengthPerRoll)
                ?? throw new NotFoundException(nameof(WarehouseStock), nameof(item.Id), item.Id);

            residue.RollCount -= item.RollCount;
            residue.TotalLength -= item.RollCount * item.LengthPerRoll;
            residue.UnitPrice = item.UnitPrice;
            residue.DiscountRate = item.DiscountRate;

            await HandleResidueAsync(item, warehouse, cancellationToken);

            var product = await context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken)
                ?? throw new NotFoundException(nameof(Product), nameof(item.Id), item.ProductId);

            if (!allowNegativeStock && (residue.RollCount < 0 || residue.TotalLength < 0))
                throw new ConflictException($"Omborda yetarli qoldiq yo'q: {product.Name} ({item.LengthPerRoll:0.###} {product.Unit}).");

            item.Weight = product.IsWeightTracked && product.WeightPerUnit is decimal weight
                ? item.TotalLength * weight
                : 0;

            descriptionBuilder.Append($"{product.Name} - {item.TotalLength:N2} x {item.UnitPrice:N2} = {item.TotalAmount:N2}");

            if (item.DiscountAmount != 0)
                descriptionBuilder.Append($" [ch: {item.DiscountRate:0.##}% = {item.DiscountAmount:N2}]");

            if (saleItems.IndexOf(item) < saleItems.Count - 1)
                descriptionBuilder.Append(";\n");
        }
    }

    private async Task HandleResidueAsync(SaleItemCommand item, Warehouse warehouse, CancellationToken cancellationToken)
    {
        var detail = item.LengthPerRoll * item.RollCount - item.TotalLength;

        // Uzunlik 3 xonada yaxlitlangani uchun to'liq rulondan biroz oshishi mumkin — bunda bo'lak qaytmaydi.
        if (detail > 0)
        {
            var existStock = await context.WarehouseStocks
                .FirstOrDefaultAsync(i => i.WarehouseId == warehouse.Id
                                       && i.ProductId == item.ProductId
                                       && i.LengthPerRoll == detail, cancellationToken);

            if (existStock is null)
                context.WarehouseStocks.Add(new WarehouseStock
                {
                    RollCount = 1,
                    ProductId = item.ProductId,
                    LengthPerRoll = detail,
                    UnitPrice = item.UnitPrice,
                    TotalLength = detail,
                    Warehouse = warehouse
                });
            else
            {
                existStock.RollCount += 1;
                existStock.UnitPrice = item.UnitPrice;
                existStock.TotalLength += detail;
            }
        }
    }

    private static void AccumulateDiscount(Account account, decimal discount, bool isApplied)
    {
        if (!isApplied)
            account.Discount += discount;
    }

    private static CustomerOperation CreateCustomerOperation(Sale sale, Account account,
                   string description, StringBuilder descriptionBuilder, bool isApplied)
    {
        if (description.Trim().Length > 0) description = description + ". ";
        return new CustomerOperation
        {
            Date = sale.Date,
            Amount = -sale.Amount,
            IsDiscountApplied = sale.IsDiscountApplied,
            Discount = sale.Discount,
            Account = account,
            AccountId = account.Id,
            CustomerId = sale.CustomerId,
            OperationType = OperationType.Sale,
            Description = $"Savdo: {description}\n" +
                          $"{descriptionBuilder}\n" +
                          $"-------------------------------------\n" +
                          $"Jami: {sale.Amount + (isApplied ? sale.Discount : 0):N2}" +
                          (sale.Discount > 0 ? $"\nChegirma: {sale.Discount:N2} {(isApplied ? "(ayrildi)" : "(saqlandi)")}" : "")
        };
    }
}
