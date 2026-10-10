namespace VoltStream.Application.Features.Users.Commands;

using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

[RequirePermission(Permissions.SettingsUsers)]
public record ChangePasswordCommand(long Id, string NewPassword) : IRequest<bool>;

public class ChangePasswordCommandHandler(IAppDbContext context, ICurrentUser current)
    : IRequestHandler<ChangePasswordCommand, bool>
{
    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), nameof(request.Id), request.Id);

        if (!current.IsAdmin && user.Role == UserRole.Admin)
            throw new ForbiddenException("Admin parolini faqat admin o'zgartira oladi.");

        if (!current.IsAdmin && !current.Has(Permissions.BranchesAll) && user.BranchId != current.BranchId)
            throw new ForbiddenException("Boshqa filial foydalanuvchisi.");

        user.PasswordHash = BCrypt.HashPassword(request.NewPassword, workFactor: 12);

        return await context.SaveAsync(cancellationToken) > 0;
    }
}
