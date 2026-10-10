namespace ApiServices.Models.Responses;

public record BranchResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
}
