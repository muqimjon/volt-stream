namespace VoltStream.Application.Features.Users.Commands;

using BCrypt.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsUsers)]
public record ChangePasswordCommand(long Id, string NewPassword) : IRequest<bool>;

public class ChangePasswordCommandHandler(IAppDbContext context)
    : IRequestHandler<ChangePasswordCommand, bool>
{
    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), nameof(request.Id), request.Id);

        user.PasswordHash = BCrypt.HashPassword(request.NewPassword, workFactor: 12);

        return await context.SaveAsync(cancellationToken) > 0;
    }
}
