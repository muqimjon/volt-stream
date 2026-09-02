namespace VoltStream.Application.Commons.Interfaces;

using VoltStream.Domain.Entities;

public interface IAuditLogWriter
{
    Task WriteAsync(AuditLog log);
}
