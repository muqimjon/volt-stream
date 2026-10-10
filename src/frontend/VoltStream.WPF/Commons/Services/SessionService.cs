namespace VoltStream.WPF.Commons.Services;

using ApiServices.Enums;
using ApiServices.Models.Responses;

public interface ISessionService
{
    UserResponse? CurrentUser { get; set; }
    string? Token => CurrentUser?.Token;
    bool IsAdmin => CurrentUser?.Role == UserRole.Admin;
    long? BranchId => CurrentUser?.BranchId;
    string? BranchName => CurrentUser?.BranchName;
    bool Has(string permission) => IsAdmin || (CurrentUser?.Permissions?.Contains(permission) ?? false);
}

public class SessionService : ISessionService
{
    public UserResponse? CurrentUser { get; set; }
}
