namespace ApiServices.Models.Responses;

public record PermissionResponse
{
    public string Key { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
}
