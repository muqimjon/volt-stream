namespace VoltStream.Domain.Entities;

using VoltStream.Domain.Enums;

public class AuditLog
{
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public long? UserId { get; set; }
    public string? Username { get; set; }
    public long? BranchId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public long? EntityId { get; set; }
    public string? Details { get; set; }
    public ClientSource Source { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
