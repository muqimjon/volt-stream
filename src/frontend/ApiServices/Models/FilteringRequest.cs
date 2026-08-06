namespace ApiServices.Models;

public record FilteringRequest
{
    public Dictionary<string, List<string>>? Filters { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string? SortBy { get; set; }
    public bool Descending { get; set; }
    public double TimeZone { get; set; } = LocalTimeZone;

    public static double LocalTimeZone
        => TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalHours;
}
