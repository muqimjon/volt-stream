namespace VoltStream.Mobile;

using System.Net.Http;

public static class ServerHealth
{
    private const string Marker = "Server is healthy";

    private static readonly HttpClient http = new(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = static (_, _, _, _) => true
    })
    { Timeout = Timeout.InfiniteTimeSpan };

    public static string? Normalize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var text = url.Trim();
        if (!text.Contains("://", StringComparison.Ordinal))
            text = $"http://{text}";

        return Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
            ? uri.GetLeftPart(UriPartial.Authority)
            : null;
    }

    public static async Task<bool> IsAliveAsync(string? url, int timeoutMs = 1500)
    {
        var root = Normalize(url);
        if (root is null)
            return false;

        try
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            using var response = await http.GetAsync($"{root}/api/health", cts.Token);
            if (!response.IsSuccessStatusCode)
                return false;

            var body = await response.Content.ReadAsStringAsync(cts.Token);
            return body.Contains(Marker, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static async Task<string?> ResolveAsync(int timeoutMs = 3000)
    {
        foreach (var url in new[] { Settings.PrimaryUrl, Settings.BackupUrl })
            if (await IsAliveAsync(url, timeoutMs))
                return Normalize(url);

        return null;
    }
}
