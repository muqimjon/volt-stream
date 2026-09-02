namespace VoltStream.Domain.Enums;

public enum PrintJobStatus
{
    Pending = 0,
    Printing = 1,
    Completed = 2,
    Failed = 3,
    Rejected = 4,
    Cancelled = 5
}
