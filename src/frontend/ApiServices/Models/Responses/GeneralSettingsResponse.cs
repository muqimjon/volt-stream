namespace ApiServices.Models.Responses;

public record GeneralSettingsResponse
{
    public bool AllowNegativeStock { get; set; } = true;
}
