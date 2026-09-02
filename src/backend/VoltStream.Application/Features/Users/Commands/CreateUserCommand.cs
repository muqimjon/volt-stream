namespace VoltStream.Application.Features.Users.Commands;

using AutoMapper;
using BCrypt.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

[RequirePermission(Commons.Security.Permissions.SettingsUsers)]
public record CreateUserCommand(
    string Username,
    string Password,
    string? Name,
    UserRole Role,
    string? Phone,
    string? Email,
    string? Address,
    DateTime? DateOfBirth,
    bool IsActive = true,
    long? BranchId = null,
    string[]? Permissions = null,
    PrintPolicy PrintPolicy = PrintPolicy.Auto)
    : IRequest<long>;

public class CreateUserCommandHandler(IAppDbContext context, IMapper mapper)
    : IRequestHandler<CreateUserCommand, long>
{
    public async Task<long> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (request.Role != UserRole.Admin && request.BranchId is null)
            throw new AppException("Admin bo'lmagan foydalanuvchiga filial biriktirilishi shart.");

        var user = mapper.Map<User>(request);

        var exitUser = await context.Users.AnyAsync(
            u => u.NormalizedUsername == user.NormalizedUsername,
            cancellationToken);

        if (exitUser)
            throw new AlreadyExistException(nameof(User), nameof(request.Username), request.Username);

        if (!string.IsNullOrEmpty(request.Email))
        {
            var exitEmail = await context.Users.AnyAsync(
                u => u.NormalizedEmail == user.NormalizedEmail,
                cancellationToken);

            if (exitEmail)
                throw new AlreadyExistException(nameof(User), nameof(request.Email), request.Email);
        }

        user.Permissions = request.Permissions ?? RolePresets.For(request.Role);
        user.PasswordHash = BCrypt.HashPassword(request.Password, workFactor: 12);

        context.Users.Add(user);
        await context.SaveAsync(cancellationToken);

        return user.Id;
    }
}