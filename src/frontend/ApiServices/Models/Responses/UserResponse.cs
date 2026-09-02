namespace ApiServices.Models.Responses;

using ApiServices.Enums;

public record UserResponse
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public long? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Phone { get; set; }
    public string[] Permissions { get; set; } = [];
    public PrintPolicy PrintPolicy { get; set; }
}
