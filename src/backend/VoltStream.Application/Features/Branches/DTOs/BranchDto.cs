namespace VoltStream.Application.Features.Branches.DTOs;

public record BranchDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
}
