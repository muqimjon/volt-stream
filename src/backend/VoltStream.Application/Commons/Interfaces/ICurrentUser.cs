namespace VoltStream.Application.Commons.Interfaces;

using VoltStream.Domain.Enums;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    bool HasHttpContext { get; }
    long? UserId { get; }
    string? Username { get; }
    long? BranchId { get; }
    bool IsAdmin { get; }
    IReadOnlySet<string> Permissions { get; }
    ClientSource Source { get; }
    bool Has(string permission) => IsAdmin || Permissions.Contains(permission);
}
