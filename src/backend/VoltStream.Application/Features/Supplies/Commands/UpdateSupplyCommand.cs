namespace VoltStream.Application.Features.Supplies.Commands;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;

public record UpdateSupplyCommand(
    long Id,
    DateTimeOffset Date,
    long CategoryId,
    string CategoryName,
    long ProductId,
    string ProductName,
    string Unit,
    int RollCount,
    decimal LengthPerRoll,
    decimal TotalLength,
    decimal UnitPrice,
    decimal DiscountRate)
    : IRequest<bool>;

public class UpdateSupplyCommandHandler(
    IAppDbContext context)
    : IRequestHandler<UpdateSupplyCommand, bool>
{
    public async Task<bool> Handle(UpdateSupplyCommand request, CancellationToken cancellationToken)
    {
        await context.BeginTransactionAsync(cancellationToken);

        try
        {
            var supply = await context.Supplies
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Supply), nameof(request.Id), request.Id);

            var oldStock = await context.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.ProductId == supply.ProductId &&
                                           ws.LengthPerRoll == supply.LengthPerRoll,
                                       cancellationToken)
                ?? throw new NotFoundException(nameof(WarehouseStock), nameof(supply.Id), supply.Id);

            var warehouseId = oldStock.WarehouseId;

            oldStock.RollCount -= supply.RollCount;
            oldStock.TotalLength -= supply.TotalLength;

            var category = await context.Categories
                .FirstOrDefaultAsync(c => c.NormalizedName == request.CategoryName.ToNormalized(), cancellationToken);
            if (category is null)
            {
                category = new Category
                {
                    Name = request.CategoryName.Trim(),
                    NormalizedName = request.CategoryName.Trim().ToUpper()
                };
                context.Categories.Add(category);
                await context.SaveAsync(cancellationToken);
            }

            var product = await context.Products
                .FirstOrDefaultAsync(p => p.NormalizedName == request.ProductName.ToNormalized(), cancellationToken);
            if (product is null)
            {
                product = new Product
                {
                    Name = request.ProductName.Trim(),
                    NormalizedName = request.ProductName.Trim().ToUpper(),
                    CategoryId = category.Id,
                    Unit = request.Unit
                };
                context.Products.Add(product);
                await context.SaveAsync(cancellationToken);
            }
            else
            {
                if (product.CategoryId != category.Id)
                {
                    product.CategoryId = category.Id;
                    if (!string.IsNullOrWhiteSpace(request.Unit) && product.Unit != request.Unit)
                        product.Unit = request.Unit;

                    context.Products.Update(product);
                    await context.SaveAsync(cancellationToken);
                }
            }

            var newStock = await context.WarehouseStocks
                .FirstOrDefaultAsync(ws => ws.ProductId == product.Id &&
                                           ws.LengthPerRoll == request.LengthPerRoll &&
                                           ws.WarehouseId == warehouseId,
                                       cancellationToken);

            if (newStock is null)
            {
                newStock = new WarehouseStock
                {
                    WarehouseId = warehouseId,
                    ProductId = product.Id,
                    RollCount = request.RollCount,
                    LengthPerRoll = request.LengthPerRoll,
                    TotalLength = request.TotalLength,
                    UnitPrice = request.UnitPrice,
                    DiscountRate = request.DiscountRate
                };
                context.WarehouseStocks.Add(newStock);
            }
            else
            {
                newStock.RollCount += request.RollCount;
                newStock.TotalLength += request.TotalLength;
                newStock.UnitPrice = request.UnitPrice;
                newStock.DiscountRate = request.DiscountRate;
            }

            supply.Date = request.Date;
            supply.RollCount = request.RollCount;
            supply.LengthPerRoll = request.LengthPerRoll;
            supply.TotalLength = request.TotalLength;
            supply.ProductId = product.Id;
            supply.UnitPrice = request.UnitPrice;
            supply.DiscountRate = request.DiscountRate;

            await context.SaveAsync(cancellationToken);
            await context.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
