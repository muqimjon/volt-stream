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

public class UpdateUserCommandHandler(IAppDbContext context, ICurrentUser current)
    : IRequestHandler<UpdateUserCommand, bool>
{
    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Role))
            throw new AppException("Rol noto'g'ri");

        if (request.Role != UserRole.Admin && request.BranchId is null)
            throw new AppException("Admin bo'lmagan foydalanuvchiga filial biriktirilishi shart.");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), nameof(request.Id), request.Id);

        var permissions = request.Permissions ?? RolePresets.For(request.Role);

        if (!current.IsAdmin)
        {
            if (request.Role == UserRole.Admin || user.Role == UserRole.Admin)
                throw new ForbiddenException("Admin foydalanuvchini faqat admin boshqara oladi.");

            if (user.Id == current.UserId && (request.Role != user.Role
                || request.IsActive != user.IsActive
                || !permissions.ToHashSet().SetEquals(user.Permissions)))
                throw new ForbiddenException("O'z rol va ruxsatlaringizni o'zgartira olmaysiz.");

            if (user.Id == current.UserId && request.BranchId != user.BranchId)
                throw new ForbiddenException("O'z filialingizni o'zgartira olmaysiz.");

            if (permissions.Any(permission => !current.Has(permission)))
                throw new ForbiddenException("O'zingizda yo'q ruxsatni bera olmaysiz.");

            if (!current.Has(Commons.Security.Permissions.BranchesAll)
                && (request.BranchId != current.BranchId || user.BranchId != current.BranchId))
                throw new ForbiddenException("Boshqa filial foydalanuvchisi.");
        }

        if (user.Role == UserRole.Admin && user.IsActive
            && (!request.IsActive || request.Role != UserRole.Admin)
            && !await context.Users.AnyAsync(
                u => u.Id != user.Id && u.Role == UserRole.Admin && u.IsActive, cancellationToken))
            throw new AppException("Tizimdagi oxirgi faol adminni o'chirib bo'lmaydi.");

        if (!string.IsNullOrEmpty(request.Email))
        {
            var normalizedEmail = request.Email.ToNormalized();
            var exitEmail = await context.Users.AnyAsync(
                u => u.Id != user.Id && u.NormalizedEmail == normalizedEmail,
                cancellationToken);

            if (exitEmail)
                throw new AlreadyExistException(nameof(User), nameof(request.Email), request.Email);
        }

        user.Name = request.Name;
        user.NormalizedName = request.Name.ToNormalized();
        user.Role = request.Role;
        user.BranchId = request.BranchId;
        user.Permissions = permissions;
        user.IsActive = request.IsActive;
        user.Phone = request.Phone;
        user.Email = request.Email;
        user.NormalizedEmail = request.Email.ToNormalized();
        user.PrintPolicy = request.PrintPolicy;

        return await context.SaveAsync(cancellationToken) > 0;
    }
}
