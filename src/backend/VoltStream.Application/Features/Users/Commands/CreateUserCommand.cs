namespace VoltStream.Application.Features.Users.Commands;

using BCrypt.Net;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
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

public class CreateUserCommandHandler(IAppDbContext context, ICurrentUser current, IMapper mapper)
    : IRequestHandler<CreateUserCommand, long>
{
    public async Task<long> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Role))
            throw new AppException("Rol noto'g'ri");

        var permissions = request.Permissions ?? RolePresets.For(request.Role);

        if (!current.IsAdmin && (current.IsAuthenticated || current.HasHttpContext))
        {
            if (request.Role == UserRole.Admin)
                throw new ForbiddenException("Admin foydalanuvchini faqat admin yarata oladi.");

            if (permissions.Any(permission => !current.Has(permission)))
                throw new ForbiddenException("O'zingizda yo'q ruxsatni bera olmaysiz.");

            if (!current.Has(Commons.Security.Permissions.BranchesAll))
                request = request with
                {
                    BranchId = current.BranchId ?? throw new ForbiddenException("Filial biriktirilmagan")
                };
        }

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

        user.Permissions = permissions;
        user.PasswordHash = BCrypt.HashPassword(request.Password, workFactor: 12);

        context.Users.Add(user);
        await context.SaveAsync(cancellationToken);

        return user.Id;
    }
}