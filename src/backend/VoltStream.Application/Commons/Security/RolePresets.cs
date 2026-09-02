namespace VoltStream.Application.Commons.Security;

using VoltStream.Domain.Enums;

public static class RolePresets
{
    private static readonly string[] staff =
        [.. Permissions.All.Where(p => !p.StartsWith("settings.") && p != Permissions.BranchesAll)];

    public static string[] For(UserRole role)
        => role == UserRole.Admin ? Permissions.All : staff;
}
