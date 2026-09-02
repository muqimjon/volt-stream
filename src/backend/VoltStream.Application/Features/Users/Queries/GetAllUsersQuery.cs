namespace VoltStream.Application.Features.Users.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Users.DTOs;

[RequirePermission(Permissions.SettingsUsers)]
public record GetAllUsersQuery : IRequest<IReadOnlyCollection<UserDto>>;

public class GetAllUsersQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllUsersQuery, IReadOnlyCollection<UserDto>>
{
    public async Task<IReadOnlyCollection<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<UserDto>>(await context.Users
            .AsNoTracking()
            .Include(u => u.Branch)
            .OrderBy(u => u.Id)
            .ToListAsync(cancellationToken));
}
