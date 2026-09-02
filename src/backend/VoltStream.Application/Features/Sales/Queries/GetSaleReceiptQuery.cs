namespace VoltStream.Application.Features.Sales.Queries;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
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
            Items = [.. sale.Items.Select(item => new SaleReceiptItemDto
            {
                Category = item.Product.Category?.Name ?? string.Empty,
                Product = item.Product.Name,
                LengthPerRoll = item.LengthPerRoll,
                RollCount = item.RollCount,
                TotalLength = item.TotalLength,
                Unit = item.Product.Unit,
                Weight = item.Weight,
                UnitPrice = hasPrices ? item.UnitPrice : null,
                TotalAmount = hasPrices ? item.FinalAmount : null
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
