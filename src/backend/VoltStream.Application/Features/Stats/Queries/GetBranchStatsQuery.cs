namespace VoltStream.Application.Features.Stats.Queries;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Stats.DTOs;
using VoltStream.Domain.Enums;

public record GetBranchStatsQuery(
    DateTimeOffset Begin,
    DateTimeOffset End,
    long? BranchId = null,
    long? UserId = null) : IRequest<BranchStatsDto>;

public class GetBranchStatsQueryHandler(IAppDbContext context, BranchContext branches, ICurrentUser user)
    : IRequestHandler<GetBranchStatsQuery, BranchStatsDto>
{
    public async Task<BranchStatsDto> Handle(GetBranchStatsQuery request, CancellationToken cancellationToken)
    {
        var offset = request.Begin.Offset;
        var begin = new DateTimeOffset(request.Begin.Date, offset).ToUniversalTime();
        var end = new DateTimeOffset(request.End.Date, offset).AddDays(1).ToUniversalTime();
        var branchId = await branches.ResolveFilterAsync(request.BranchId, true, cancellationToken);

        var sales = context.Sales.AsNoTracking().Where(s => s.Date >= begin && s.Date < end);
        var supplies = context.Supplies.AsNoTracking().Where(s => s.Date >= begin && s.Date < end);
        var drafts = context.SaleDrafts.AsNoTracking().Where(d => d.Date >= begin && d.Date < end);

        if (branchId is not null)
        {
            sales = sales.Where(s => s.BranchId == branchId);
            supplies = supplies.Where(s => s.BranchId == branchId);
            drafts = drafts.Where(d => d.BranchId == branchId);
        }

        var userId = user.Has(Permissions.ReportsView) ? request.UserId : user.UserId;

        if (userId is not null)
        {
            sales = sales.Where(s => s.CreatedByUserId == userId);
            supplies = supplies.Where(s => s.CreatedByUserId == userId);
            drafts = drafts.Where(d => d.CreatedByUserId == userId);
        }

        var draftStatuses = await drafts
            .GroupBy(d => d.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, cancellationToken);

        return new BranchStatsDto
        {
            IsOwnScope = userId is not null,
            SalesCount = await sales.CountAsync(cancellationToken),
            SoldRolls = await sales.SumAsync(s => s.RollCount, cancellationToken),
            SoldLength = await sales.SumAsync(s => s.Length, cancellationToken),
            SoldWeight = await context.SaleItems.AsNoTracking()
                .Where(i => sales.Any(s => s.Id == i.SaleId))
                .SumAsync(i => i.Weight, cancellationToken),
            SuppliesCount = await supplies.CountAsync(cancellationToken),
            ReceivedRolls = await supplies.SumAsync(s => s.RollCount, cancellationToken),
            ReceivedLength = await supplies.SumAsync(s => s.TotalLength, cancellationToken),
            ReceivedWeight = await supplies
                .Where(s => s.Product.IsWeightTracked && s.Product.WeightPerUnit != null)
                .SumAsync(s => s.TotalLength * s.Product.WeightPerUnit!.Value, cancellationToken),
            DraftsCount = draftStatuses.Values.Sum(),
            DraftsOpen = draftStatuses.GetValueOrDefault(SaleDraftStatus.Open),
            DraftsConfirmed = draftStatuses.GetValueOrDefault(SaleDraftStatus.Confirmed),
            DraftsCancelled = draftStatuses.GetValueOrDefault(SaleDraftStatus.Cancelled),
            DraftRolls = await drafts.SelectMany(d => d.Items).SumAsync(i => i.RollCount, cancellationToken),
            DraftLength = await drafts.SelectMany(d => d.Items).SumAsync(i => i.TotalLength, cancellationToken)
        };
    }
}
