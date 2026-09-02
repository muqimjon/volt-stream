namespace VoltStream.WebApi.Hubs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Enums;

[Authorize]
public class PrintHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var principal = Context.User;

        if (principal?.FindFirstValue("sub") is string userId)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        if (IsAgent(principal) && BranchId(principal) is string branchId)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"branch-{branchId}");

        await base.OnConnectedAsync();
    }

    private static bool IsAgent(ClaimsPrincipal? principal)
        => principal?.IsInRole(nameof(UserRole.Admin)) == true
        || principal?.FindAll("permission").Any(claim => claim.Value == Permissions.PrintAgent) == true;

    private string? BranchId(ClaimsPrincipal? principal)
        => principal?.FindFirstValue("branch")
        ?? (HasAllBranches(principal) ? Context.GetHttpContext()?.Request.Query["branchId"].FirstOrDefault() : null);

    private static bool HasAllBranches(ClaimsPrincipal? principal)
        => principal?.IsInRole(nameof(UserRole.Admin)) == true
        || principal?.FindAll("permission").Any(claim => claim.Value == Permissions.BranchesAll) == true;
}
