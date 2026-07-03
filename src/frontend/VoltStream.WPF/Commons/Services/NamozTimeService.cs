namespace VoltStream.WPF.Commons.Services;

using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

public class NamozTimeService
{
    private const string ApiUrl = "https://namoz-vaqti.uz/?format=json&lang=lotin&period=month&region=qoqon-shahri";
    private static readonly string CachePath = Path.Combine(AppContext.BaseDirectory, "namoz_times.json");
    private static readonly HttpClient _httpClient = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<NamozApiResponse?> GetFullDataAsync()
    {
        NamozApiResponse? cachedData = null;

        if (File.Exists(CachePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(CachePath);
                cachedData = JsonSerializer.Deserialize<NamozApiResponse>(json, _jsonOptions);
            }
            catch { cachedData = null; }
        }

        if (cachedData?.PeriodTable != null && cachedData.PeriodTable.Count != 0)
        {
            var lastDateStr = cachedData.PeriodTable.Last().Date;
            if (DateTime.TryParseExact(lastDateStr, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var lastDate))
            {
                var daysRemaining = (lastDate.Date - DateTime.Now.Date).Days;

                if (daysRemaining >= 0)
                {
                    if (daysRemaining <= 10)
                        _ = RefreshCacheAsync();

                    return cachedData;
                }
            }
        }

        return await RefreshCacheAsync();
    }

    private async Task<NamozApiResponse?> RefreshCacheAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<NamozApiResponse>(ApiUrl, _jsonOptions);
            if (response != null)
            {
                var json = JsonSerializer.Serialize(response, _jsonOptions);
                await File.WriteAllTextAsync(CachePath, json);
            }
            return response;
        }
        catch { return null; }
    }
}