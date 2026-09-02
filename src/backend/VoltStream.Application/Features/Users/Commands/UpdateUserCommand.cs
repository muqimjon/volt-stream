namespace VoltStream.Application.Features.Users.Commands;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

[RequirePermission(Commons.Security.Permissions.SettingsUsers)]
public record UpdateUserCommand(
    long Id,
    string? Name,
    UserRole Role,
    long? BranchId,
    string[]? Permissions,
    bool IsActive,
    string? Phone,
    string? Email,
    PrintPolicy PrintPolicy)
    : IRequest<bool>;

public class UpdateUserCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateUserCommand, bool>
{
    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), nameof(request.Id), request.Id);

        user.Name = request.Name;
        user.NormalizedName = request.Name.ToNormalized();
        user.Role = request.Role;
        user.BranchId = request.BranchId;
        user.Permissions = request.Permissions ?? RolePresets.For(request.Role);
        user.IsActive = request.IsActive;
        user.Phone = request.Phone;
        user.Email = request.Email;
        user.NormalizedEmail = request.Email.ToNormalized();
        user.PrintPolicy = request.PrintPolicy;

        return await context.SaveAsync(cancellationToken) > 0;
    }
}
