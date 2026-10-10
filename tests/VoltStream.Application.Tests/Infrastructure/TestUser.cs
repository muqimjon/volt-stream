namespace VoltStream.Application.Tests.Infrastructure;

using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Enums;

public sealed class TestUser : ICurrentUser
{
    private static long lastId = 1000;

    private TestUser(long id, long? branchId, bool isAdmin, IEnumerable<string> permissions)
    {
        UserId = id;
        BranchId = branchId;
        IsAdmin = isAdmin;
        Permissions = permissions.ToHashSet();
    }

    public bool IsAuthenticated => true;
    public bool HasHttpContext => true;
    public long? UserId { get; }
    public string? Username => $"user{UserId}";
    public long? BranchId { get; }
    public bool IsAdmin { get; }
    public IReadOnlySet<string> Permissions { get; }
    public ClientSource Source => ClientSource.Desktop;

    public static TestUser Admin() => Of(UserRole.Admin, null);

    public static TestUser Of(UserRole role, long? branchId)
        => new(Interlocked.Increment(ref lastId), branchId, role == UserRole.Admin, RolePresets.For(role));

    public static TestUser With(long? branchId, params string[] permissions)
        => new(Interlocked.Increment(ref lastId), branchId, false, permissions);

    // Users jadvalida qatori bor foydalanuvchi (masalan, chop etish so'rovchisi bazadan qidiriladi)
    public static TestUser Existing(long id, long? branchId, IEnumerable<string> permissions)
        => new(id, branchId, false, permissions);
}

public sealed class CurrentUserHolder
{
    public TestUser User { get; set; } = default!;
}
