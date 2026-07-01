namespace VoltStream.WPF.Configurations;

using System.Net.Http;

public static class ServerHealth
{
    private const int TimeoutMs = 1500;
    private const string Marker = "Server is healthy";

    public static async Task<bool> IsAliveAsync(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) ||
            !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            uri.Host.Equals("example.com", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = static (_, _, _, _) => true,
                AllowAutoRedirect = true
            };
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(TimeoutMs) };
            using var response = await http.GetAsync($"{uri.GetLeftPart(UriPartial.Authority)}/api/health");
            if (!response.IsSuccessStatusCode)
                return false;

            var body = await response.Content.ReadAsStringAsync();
            return body.Contains(Marker, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
