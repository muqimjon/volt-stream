namespace VoltStream.Application.Features.Supplies.Commands;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SuppliesCreate)]
public record CreateSupplyCommand(
    DateTimeOffset Date,
    long CategoryId,
    string CategoryName,
    long ProductId,
    string ProductName,
    int RollCount,
    string Unit,
    decimal LengthPerRoll,
    decimal TotalLength,
    decimal? UnitPrice,
    decimal? DiscountRate,
    long? BranchId = null)
    : IRequest<long>;

public class CreateSupplyCommandHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IMapper mapper)
    : IRequestHandler<CreateSupplyCommand, long>
{
    public async Task<long> Handle(CreateSupplyCommand request, CancellationToken cancellationToken)
    {
        var branchId = await branches.ResolveAsync(request.BranchId, cancellationToken);
        var canSetPrice = user.Has(Permissions.SalesViewPrices);
        var requestedPrice = canSetPrice ? request.UnitPrice : null;
        var requestedDiscount = canSetPrice ? request.DiscountRate : null;

        var warehouse = await context.Warehouses
            .Include(w => w.Stocks)
            .FirstOrDefaultAsync(w => w.BranchId == branchId, cancellationToken);

        if (warehouse is null)
        {
            context.Warehouses.Add(warehouse = new() { BranchId = branchId });
            warehouse.Stocks = [];
        }

        await context.BeginTransactionAsync(cancellationToken);

        long newCategoryId = request.CategoryId;
        if (request.CategoryId <= 0)
        {
            var category = new Category()
            {
                Name = request.CategoryName.Trim(),
                NormalizedName = request.CategoryName.ToNormalized(),
            };
            context.Categories.Add(category);
            await context.SaveAsync(cancellationToken);
            newCategoryId = category.Id;
        }

        long newProductId = request.ProductId;
        if (request.ProductId <= 0)
        {
            var product = new Product()
            {
                Name = request.ProductName.Trim(),
                NormalizedName = request.ProductName.Trim().ToUpper(),
                CategoryId = newCategoryId,
                Unit = request.Unit
            };

            context.Products.Add(product);
            await context.SaveAsync(cancellationToken);
            newProductId = product.Id;
        }

        var warehouseItem = warehouse.Stocks.FirstOrDefault(wh
            => wh.ProductId == newProductId && wh.LengthPerRoll == request.LengthPerRoll);


        decimal unitPrice;
        decimal discountRate;

        if (warehouseItem is null)
        {
            unitPrice = requestedPrice ?? 0;
            discountRate = requestedDiscount ?? 0;

            var stock = mapper.Map<WarehouseStock>(request);
            stock.ProductId = newProductId;
            stock.UnitPrice = unitPrice;
            stock.DiscountRate = discountRate;
            warehouse.Stocks.Add(stock);
        }
        else
        {
            unitPrice = requestedPrice ?? warehouseItem.UnitPrice;
            discountRate = requestedDiscount ?? warehouseItem.DiscountRate;

            warehouseItem.RollCount += request.RollCount;
            warehouseItem.TotalLength += request.TotalLength;
            warehouseItem.UnitPrice = unitPrice;
            warehouseItem.DiscountRate = discountRate;
        }

        var supply = mapper.Map<Supply>(request);
        supply.ProductId = newProductId;
        supply.BranchId = branchId;
        supply.UnitPrice = unitPrice;
        supply.DiscountRate = discountRate;
        context.Supplies.Add(supply);

        await context.CommitTransactionAsync(cancellationToken);
        return supply.Id;
    }
}
