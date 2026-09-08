namespace VoltStream.Application.Features.Sales.Queries;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SalesView)]
public record GetSaleReceiptQuery(long Id) : IRequest<SaleReceiptDto>;

public class GetSaleReceiptQueryHandler(IAppDbContext context, ICurrentUser user)
    : IRequestHandler<GetSaleReceiptQuery, SaleReceiptDto>
{
    public async Task<SaleReceiptDto> Handle(GetSaleReceiptQuery request, CancellationToken cancellationToken)
    {
        var sale = await context.Sales
            .AsNoTracking()
            .Include(s => s.Branch)
            .Include(s => s.Customer)
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Sale), nameof(request.Id), request.Id);

        if (!user.Has(Permissions.BranchesAll) && sale.BranchId != user.BranchId)
            throw new ForbiddenException("Boshqa filial savdosi.");

        if (!user.Has(Permissions.ReportsView) && sale.CreatedByUserId != user.UserId)
            throw new ForbiddenException("Boshqa xodim savdosi.");

        var hasPrices = user.Has(Permissions.SalesViewPrices);

        var operatorName = sale.CreatedByUserId is long userId
            ? await context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return new SaleReceiptDto
        {
            SaleId = sale.Id,
            Date = sale.Date,
            Branch = sale.Branch?.Name ?? string.Empty,
            Customer = sale.Customer?.Name,
            Operator = operatorName,
            Items = [.. sale.Items.GroupBy(item => item.ProductId).Select(group =>
            {
                var product = group.First().Product;
                var lengths = group.Select(item => item.LengthPerRoll).Distinct().ToList();
                var prices = group.Select(item => item.UnitPrice).Distinct().ToList();
                return new SaleReceiptItemDto
                {
                    Category = product.Category?.Name ?? string.Empty,
                    Product = product.Name,
                    LengthPerRoll = lengths.Count == 1 ? lengths[0] : 0,
                    RollCount = group.Sum(item => item.RollCount),
                    TotalLength = group.Sum(item => item.TotalLength),
                    Composition = RollComposition.Format(RollComposition.Merge(
                        group.SelectMany(item => RollComposition.Of(item.RollCount, item.LengthPerRoll, item.TotalLength)))),
                    Unit = product.Unit,
                    Weight = group.Sum(item => item.Weight),
                    WeightPerUnit = product.IsWeightTracked ? product.WeightPerUnit : null,
                    UnitPrice = hasPrices && prices.Count == 1 ? prices[0] : null,
                    TotalAmount = hasPrices ? group.Sum(item => item.FinalAmount) : null
                };
            })],
            TotalRolls = sale.Items.Sum(item => item.RollCount),
            TotalLength = sale.Items.Sum(item => item.TotalLength),
            TotalWeight = sale.Items.Sum(item => item.Weight),
            HasPrices = hasPrices,
            Amount = hasPrices ? sale.Amount : null,
            Discount = hasPrices ? sale.Discount : null
        };
    }
}
