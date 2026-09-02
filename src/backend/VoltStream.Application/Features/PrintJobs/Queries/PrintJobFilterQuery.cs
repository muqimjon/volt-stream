namespace VoltStream.Application.Features.PrintJobs.Queries;

using MediatR;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.PrintJobs.DTOs;

public record PrintJobFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<PrintJobDto>>
{
    public long? BranchId { get; set; }
}

public class PrintJobFilterQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IPagingMetadataWriter writer) : IRequestHandler<PrintJobFilterQuery, IReadOnlyCollection<PrintJobDto>>
{
    public async Task<IReadOnlyCollection<PrintJobDto>> Handle(PrintJobFilterQuery request, CancellationToken cancellationToken)
    {
        var branchId = await branches.ResolveFilterAsync(request.BranchId, true, cancellationToken);
        var query = context.PrintJobs.AsQueryable();

        if (branchId is not null)
            query = query.Where(job => job.BranchId == branchId);

        if (!user.Has(Permissions.PrintAgent))
            query = query.Where(job => job.RequestedByUserId == user.UserId);

        return [.. await (await query.ToPagedListAsync(request, writer, cancellationToken))
            .ToDtosAsync(context, cancellationToken)];
    }
}
