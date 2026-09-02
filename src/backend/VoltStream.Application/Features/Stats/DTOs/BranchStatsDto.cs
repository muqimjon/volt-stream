namespace VoltStream.Application.Features.Stats.DTOs;

public record BranchStatsDto
{
    public int SalesCount { get; set; }
    public int SoldRolls { get; set; }
    public decimal SoldLength { get; set; }
    public decimal SoldWeight { get; set; }
    public int SuppliesCount { get; set; }
    public int ReceivedRolls { get; set; }
    public decimal ReceivedLength { get; set; }
    public decimal ReceivedWeight { get; set; }
}
