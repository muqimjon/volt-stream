namespace VoltStream.Application.Features.PrintJobs.Queries;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.PrintJobs.DTOs;
using VoltStream.Domain.Enums;

[RequirePermission(Permissions.PrintAgent)]
public record GetPendingPrintJobsQuery(long? BranchId = null) : IRequest<IReadOnlyCollection<PrintJobDto>>;

public class GetPendingPrintJobsQueryHandler(
    IAppDbContext context,
    BranchContext branches) : IRequestHandler<GetPendingPrintJobsQuery, IReadOnlyCollection<PrintJobDto>>
{
    public async Task<IReadOnlyCollection<PrintJobDto>> Handle(GetPendingPrintJobsQuery request, CancellationToken cancellationToken)
    {
        var branchId = await branches.ResolveFilterAsync(request.BranchId, true, cancellationToken);

        var query = context.PrintJobs
            .AsNoTracking()
            .Where(job => job.Status == PrintJobStatus.Pending);

        if (branchId is not null)
            query = query.Where(job => job.BranchId == branchId);

        return [.. await (await query.OrderBy(job => job.Id).ToListAsync(cancellationToken))
            .ToDtosAsync(context, cancellationToken)];
    }
}
