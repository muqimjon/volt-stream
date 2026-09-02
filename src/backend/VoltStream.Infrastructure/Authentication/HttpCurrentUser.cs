namespace VoltStream.Infrastructure.Authentication;

using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Enums;

public class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public long? UserId => long.TryParse(Principal?.FindFirstValue("sub"), out var id) ? id : null;

    public string? Username => Principal?.FindFirstValue("unique_name");

    public long? BranchId => long.TryParse(Principal?.FindFirstValue("branch"), out var id) ? id : null;

    public bool IsAdmin => Principal?.IsInRole(nameof(UserRole.Admin)) == true;

    public IReadOnlySet<string> Permissions =>
        Principal?.FindAll("permission").Select(c => c.Value).ToHashSet() ?? [];

    public ClientSource Source =>
        Enum.TryParse<ClientSource>(accessor.HttpContext?.Request.Headers["X-Client"], true, out var s) ? s : ClientSource.Desktop;
}
