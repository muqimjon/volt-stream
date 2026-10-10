namespace VoltStream.Application.Features.SaleDrafts.Queries;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.SaleDrafts.DTOs;
using VoltStream.Domain.Enums;

public record GetOpenSaleDraftsQuery(long? BranchId = null) : IRequest<IReadOnlyList<SaleDraftDto>>;

public class GetOpenSaleDraftsQueryHandler(IAppDbContext context, ICurrentUser user, BranchContext branches)
    : IRequestHandler<GetOpenSaleDraftsQuery, IReadOnlyList<SaleDraftDto>>
{
    public async Task<IReadOnlyList<SaleDraftDto>> Handle(GetOpenSaleDraftsQuery request, CancellationToken cancellationToken)
    {
        if (!user.CanUseDrafts())
            throw new ForbiddenException($"Ruxsat yetarli emas: {Permissions.DraftsManage}");

        var branchId = await branches.ResolveFilterAsync(request.BranchId, true, cancellationToken);
        var query = context.SaleDrafts.AsNoTracking().Where(d => d.Status == SaleDraftStatus.Open);

        if (branchId is not null)
            query = query.Where(d => d.BranchId == branchId);

        if (!user.SeesWholeBranch())
            query = query.Where(d => d.CreatedByUserId == user.UserId);

        var drafts = await query
            .WithDetails()
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.Id)
            .ToListAsync(cancellationToken);

        return await drafts.ToDtosAsync(context, cancellationToken);
    }
}
