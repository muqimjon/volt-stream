namespace VoltStream.Application.Features.Users.Queries;

using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Users.DTOs;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsUsers)]
public record GetUserByIdQuery(long Id) : IRequest<UserDto>;

public class GetUserByIdQueryHandler(
    IAppDbContext context,
    ICurrentUser current,
    IMapper mapper)
    : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), nameof(request.Id), request.Id);

        if (!current.IsAdmin && !current.Has(Permissions.BranchesAll) && user.BranchId != current.BranchId)
            throw new ForbiddenException("Boshqa filial foydalanuvchisi.");

        return mapper.Map<UserDto>(user);
    }
}
