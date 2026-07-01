namespace ApiServices.Handlers;

using System.Text.Json;
using ApiServices.Models;

public sealed class PagingHeaderHandler : DelegatingHandler
{
    private static readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.Headers.TryGetValues("X-Paging", out var values))
        {
            var json = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var meta = JsonSerializer.Deserialize<PagedListMetadata>(json, options);
                    if (meta is not null) PagingScope.Capture(meta);
                }
                catch { }
            }
        }

        return response;
    }
}
