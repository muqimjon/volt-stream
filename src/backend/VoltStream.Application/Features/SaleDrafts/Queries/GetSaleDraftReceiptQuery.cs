namespace VoltStream.Application.Features.SaleDrafts.Queries;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Domain.Entities;

public record GetSaleDraftReceiptQuery(long Id) : IRequest<SaleReceiptDto>;

public class GetSaleDraftReceiptQueryHandler(IAppDbContext context, ICurrentUser user, BranchContext branches)
    : IRequestHandler<GetSaleDraftReceiptQuery, SaleReceiptDto>
{
    public async Task<SaleReceiptDto> Handle(GetSaleDraftReceiptQuery request, CancellationToken cancellationToken)
    {
        var draft = await context.SaleDrafts
            .AsNoTracking()
            .WithDetails()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SaleDraft), nameof(request.Id), request.Id);

        branches.EnsureAccess(draft.BranchId);

        // Filial agenti ro'yxat ruxsatisiz ham o'z filiali ro'yxatini chop eta olishi kerak.
        if (!user.Has(Permissions.PrintAgent) && !user.CanAccess(draft, edit: false))
            throw new ForbiddenException("Boshqa xodim ro'yxati.");

        var operatorName = draft.CreatedByUserId is long userId
            ? await context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var items = draft.Items.GroupBy(item => item.ProductId).Select(group =>
        {
            var product = group.First().Product;
            var lengths = group.Select(item => item.LengthPerRoll).Distinct().ToList();
            var totalLength = group.Sum(item => item.TotalLength);
            var rolls = RollComposition.Merge(
                group.SelectMany(item => RollComposition.Of(item.RollCount, item.LengthPerRoll, item.TotalLength)));

            return new SaleReceiptItemDto
            {
                Category = product.Category?.Name ?? string.Empty,
                Product = product.Name,
                LengthPerRoll = lengths.Count == 1 ? lengths[0] : 0,
                RollCount = group.Sum(item => item.RollCount),
                TotalLength = totalLength,
                Composition = RollComposition.Format(rolls),
                Rolls = [.. rolls.Select(roll => new RollGroupDto(roll.Length, roll.Count))],
                Unit = product.Unit,
                Weight = product.IsWeightTracked && product.WeightPerUnit is decimal weight ? totalLength * weight : 0,
                WeightPerUnit = product.IsWeightTracked ? product.WeightPerUnit : null
            };
        }).ToList();

        return new SaleReceiptDto
        {
            SaleDraftId = draft.Id,
            Date = draft.Date,
            Branch = draft.Branch?.Name ?? string.Empty,
            Customer = draft.Customer?.Name,
            Operator = operatorName,
            Items = items,
            TotalRolls = items.Sum(item => item.RollCount),
            TotalLength = items.Sum(item => item.TotalLength),
            TotalWeight = items.Sum(item => item.Weight)
        };
    }
}
