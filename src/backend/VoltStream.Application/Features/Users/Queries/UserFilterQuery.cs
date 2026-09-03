namespace VoltStream.Application.Features.Users.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Users.DTOs;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsUsers)]
public record UserFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<UserDto>>;

public class UserFilterQueryHandler(
    IAppDbContext context,
    ICurrentUser current,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<UserFilterQuery, IReadOnlyCollection<UserDto>>
{
    public async Task<IReadOnlyCollection<UserDto>> Handle(UserFilterQuery request, CancellationToken cancellationToken)
    {
        IQueryable<User> query = context.Users.Include(u => u.Branch);

        if (!current.IsAdmin && !current.Has(Permissions.BranchesAll))
            query = query.Where(u => u.BranchId == current.BranchId);

        return mapper.Map<IReadOnlyCollection<UserDto>>(await query.ToPagedListAsync(request, writer, cancellationToken));
    }
}
