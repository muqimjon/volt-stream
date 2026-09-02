namespace VoltStream.Application.Features.Users.Commands;

using BCrypt.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Users.DTOs;
using System.Net;

public record LoginUserCommand(
    string Username,
    string Password) : IRequest<AuthResponse>;

public class LoginUserCommandHandler(IAppDbContext context, IJwtProvider jwtProvider)
    : IRequestHandler<LoginUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        if (!AppReadiness.Ready)
            throw new AppException("Server tayyorlanmoqda, birozdan so'ng urinib ko'ring", HttpStatusCode.ServiceUnavailable);

        var user = await context.Users
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.NormalizedUsername == request.Username.ToNormalized(), cancellationToken)
            ?? throw new NotFoundException("Login yoki parol xato!");

        if (!BCrypt.Verify(request.Password, user.PasswordHash))
            throw new NotFoundException("Login yoki parol xato!");

        if (!user.IsActive)
            throw new ForbiddenException("Foydalanuvchi faol emas!");

        user.LastLoginDate = DateTimeOffset.UtcNow;
        await context.SaveAsync(cancellationToken);

        var token = jwtProvider.Generate(user);
        return new AuthResponse(user.Id, user.Username, token, user.Role,
            user.BranchId, user.Branch?.Name, user.Permissions, user.PrintPolicy);
    }
}
