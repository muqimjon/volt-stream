namespace VoltStream.Application.Features.Users.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Users.DTOs;

[RequirePermission(Permissions.SettingsUsers)]
public record UserFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<UserDto>>;

public class UserFilterQueryHandler(
    IAppDbContext context,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<UserFilterQuery, IReadOnlyCollection<UserDto>>
{
    public async Task<IReadOnlyCollection<UserDto>> Handle(UserFilterQuery request, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<UserDto>>(await context.Users
            .Include(u => u.Branch)
            .ToPagedListAsync(request, writer, cancellationToken));
}
