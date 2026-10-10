namespace VoltStream.Application.Features.Stats.DTOs;

public record BranchStatsDto
{
    public bool IsOwnScope { get; set; }
    public int SalesCount { get; set; }
    public int SoldRolls { get; set; }
    public decimal SoldLength { get; set; }
    public decimal SoldWeight { get; set; }
    public int SuppliesCount { get; set; }
    public int ReceivedRolls { get; set; }
    public decimal ReceivedLength { get; set; }
    public decimal ReceivedWeight { get; set; }
    public int DraftsCount { get; set; }
    public int DraftsOpen { get; set; }
    public int DraftsConfirmed { get; set; }
    public int DraftsCancelled { get; set; }
    public int DraftRolls { get; set; }
    public decimal DraftLength { get; set; }
}
