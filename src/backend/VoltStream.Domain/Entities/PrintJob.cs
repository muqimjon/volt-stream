namespace VoltStream.Domain.Entities;

using VoltStream.Domain.Enums;

public class PrintJob : Auditable
{
    public long? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public long? SaleDraftId { get; set; }
    public SaleDraft? SaleDraft { get; set; }

    public long BranchId { get; set; }
    public long RequestedByUserId { get; set; }
    public PrintJobStatus Status { get; set; }
    public long? AgentUserId { get; set; }
    public string? PrinterName { get; set; }
    public int Copies { get; set; } = 1;
    public int Attempts { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}
