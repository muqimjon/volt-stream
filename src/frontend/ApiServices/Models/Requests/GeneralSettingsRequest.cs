namespace ApiServices.Models.Requests;

public record GeneralSettingsRequest
{
    public bool AllowNegativeStock { get; set; }
}
