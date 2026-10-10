namespace VoltStream.Application.Features.PrintJobs.DTOs;

using VoltStream.Domain.Enums;

public record PrintJobDto
{
    public long Id { get; set; }
    public long? SaleId { get; set; }
    public long? SaleDraftId { get; set; }
    public long BranchId { get; set; }
    public PrintJobStatus Status { get; set; }
    public int Copies { get; set; }
    public int Attempts { get; set; }
    public string? PrinterName { get; set; }
    public string? Error { get; set; }
    public string? RequestedBy { get; set; }
    public long RequestedByUserId { get; set; }
    public PrintPolicy PrintPolicy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}
