namespace VoltStream.Domain.Entities;

public abstract class Auditable
{
    public long Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long? CreatedByUserId { get; set; }
}
