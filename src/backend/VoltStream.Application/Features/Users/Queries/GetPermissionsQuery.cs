namespace VoltStream.Application.Features.Users.Queries;

using MediatR;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Users.DTOs;

public record GetPermissionsQuery : IRequest<IReadOnlyCollection<PermissionDto>>;

public class GetPermissionsQueryHandler
    : IRequestHandler<GetPermissionsQuery, IReadOnlyCollection<PermissionDto>>
{
    public Task<IReadOnlyCollection<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<PermissionDto>>(
            [.. Permissions.All.Select(p => new PermissionDto(p, p[..p.IndexOf('.')]))]);
}
