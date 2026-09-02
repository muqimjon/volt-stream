namespace ApiServices.Models.Requests;

public record BranchRequest
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
