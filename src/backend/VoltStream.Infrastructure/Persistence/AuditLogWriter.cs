namespace VoltStream.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;

public class AuditLogWriter(
    DbContextOptions<AppDbContext> options,
    ILogger<AuditLogWriter> logger) : IAuditLogWriter
{
    public async Task WriteAsync(AuditLog log)
    {
        try
        {
            await using var context = new AppDbContext(options);
            context.Set<AuditLog>().Add(log);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit log yozilmadi: {Action}", log.Action);
        }
    }
}
