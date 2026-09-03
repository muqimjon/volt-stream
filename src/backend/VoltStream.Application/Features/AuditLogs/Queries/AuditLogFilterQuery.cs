namespace VoltStream.Application.Features.AuditLogs.Queries;

using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.AuditLogs.DTOs;

public record AuditLogFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<AuditLogDto>>
{
    public long? BranchId { get; set; }
}

public class AuditLogFilterQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<AuditLogFilterQuery, IReadOnlyCollection<AuditLogDto>>
{
    public async Task<IReadOnlyCollection<AuditLogDto>> Handle(AuditLogFilterQuery request, CancellationToken cancellationToken)
    {
        var query = context.AuditLogs.AsQueryable();

        if (user.Has(Permissions.SettingsAudit))
        {
            var branchId = await branches.ResolveFilterAsync(request.BranchId, true, cancellationToken);

            if (branchId is not null)
                query = query.Where(log => log.BranchId == branchId);
        }
        else if (user.Has(Permissions.LogsViewOwn))
            query = query.Where(log => log.UserId == user.UserId);
        else
            throw new ForbiddenException($"Ruxsat yetarli emas: {Permissions.LogsViewOwn}");

        var logs = mapper.Map<IReadOnlyCollection<AuditLogDto>>(
            await query.ToPagedListAsync(request, writer, cancellationToken));

        var names = await context.Branches
            .AsNoTracking()
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        foreach (var log in logs)
            if (log.BranchId is long id && names.TryGetValue(id, out var name))
                log.BranchName = name;

        return logs;
    }
}
