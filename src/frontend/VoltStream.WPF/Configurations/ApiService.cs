namespace VoltStream.WPF.Configurations;

using ApiServices.Handlers;
using ApiServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;
using VoltStream.WPF.Commons.ViewModels;

public static class ApiService
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter() }
                })
        };

        var store = new ApiConnectionStore();
        var apiConnection = store.Load();
        store.BindAutoSave(apiConnection);

        var rawUrl = apiConnection.Url.Trim();
        apiConnection.Url = string.IsNullOrWhiteSpace(rawUrl) ||
            !Uri.IsWellFormedUriString(rawUrl, UriKind.Absolute)
            ? "https://example.com/"
            : rawUrl;

        services.AddSingleton(store);
        services.AddSingleton(apiConnection);
        services.AddSingleton<ConnectionTester>();
        services.AddTransient<AuthHeaderHandler>();
        services.AddTransient<PagingHeaderHandler>();

        typeof(IHealthCheckApi).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.EndsWith("Api"))
            .ToList()
            .ForEach(apiType =>
            {
                services.AddRefitClient(apiType, refitSettings)
                    .ConfigureHttpClient((provider, client) =>
                    {
                        var state = provider.GetRequiredService<ApiConnectionViewModel>();
                        client.BaseAddress = new Uri(state.Url + "api");
                    })
                    .AddHttpMessageHandler<AuthHeaderHandler>()
                    .AddHttpMessageHandler<PagingHeaderHandler>();
            });

        return services;
    }
}

