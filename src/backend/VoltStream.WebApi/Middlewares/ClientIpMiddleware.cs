namespace VoltStream.WebApi.Middlewares;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;
using VoltStream.WebApi.Utils;

public class ClientIpMiddleware(RequestDelegate next)
{
    private static readonly SemaphoreSlim registrationGate = new(1, 1);
    private static readonly TimeSpan lastRequestThrottle = TimeSpan.FromMinutes(1);

    public async Task InvokeAsync(HttpContext context, IAppDbContext db)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrWhiteSpace(ip))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden: No IP detected.");
            return;
        }

        if (IpHelper.IsLocal(context.Connection.RemoteIpAddress))
        {
            await next(context);
            return;
        }

        var allowed = await db.AllowedClients.FirstOrDefaultAsync(c => c.IpAddress == ip);

        if (allowed is null)
        {
            await registrationGate.WaitAsync();
            try
            {
                allowed = await db.AllowedClients.FirstOrDefaultAsync(c => c.IpAddress == ip);
                if (allowed is null)
                {
                    allowed = new AllowedClient
                    {
                        IpAddress = ip,
                        IsActive = false,
                        CreatedAt = DateTime.UtcNow,
                        LastRequestAt = DateTime.UtcNow
                    };
                    db.AllowedClients.Add(allowed);
                    await db.SaveAsync(default);
                }
            }
            finally
            {
                registrationGate.Release();
            }
        }
        else if (DateTime.UtcNow - allowed.LastRequestAt > lastRequestThrottle)
        {
            allowed.LastRequestAt = DateTime.UtcNow;
            await db.SaveAsync(default);
        }

        if (!allowed.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden: IP is blocked.");
            return;
        }

        await next(context);
    }
}
