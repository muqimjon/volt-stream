namespace VoltStream.Application.Features.Users.Queries;

using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Users.DTOs;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsUsers)]
public record GetAllUsersQuery : IRequest<IReadOnlyCollection<UserDto>>;

public class GetAllUsersQueryHandler(
    IAppDbContext context,
    ICurrentUser current,
    IMapper mapper)
    : IRequestHandler<GetAllUsersQuery, IReadOnlyCollection<UserDto>>
{
    public async Task<IReadOnlyCollection<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        IQueryable<User> query = context.Users.AsNoTracking().Include(u => u.Branch);

        if (!current.IsAdmin && !current.Has(Permissions.BranchesAll))
            query = query.Where(u => u.BranchId == current.BranchId);

        return mapper.Map<IReadOnlyCollection<UserDto>>(await query
            .OrderBy(u => u.Id)
            .ToListAsync(cancellationToken));
    }
}
