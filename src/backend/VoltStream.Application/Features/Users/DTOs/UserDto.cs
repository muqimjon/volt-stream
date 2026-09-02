namespace VoltStream.Application.Features.Users.DTOs;

using VoltStream.Domain.Enums;

public record UserDto(
    long Id,
    string Username,
    string? Name,
    UserRole Role,
    long? BranchId,
    string? BranchName,
    string[] Permissions,
    bool IsActive,
    PrintPolicy PrintPolicy,
    string? Phone);
