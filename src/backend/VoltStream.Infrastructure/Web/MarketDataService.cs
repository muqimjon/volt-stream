namespace VoltStream.Infrastructure.Web;

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Dashboard.DTOs;

/// <summary>
/// Valyuta kurslari (CBU rasmiy JSON API) va metall narxlarini (investing.com HTML sahifasi)
/// oladi. Natija 15 daqiqaga keshlanadi - tashqi saytlarga har so'rovda murojaat qilinmaydi.
/// Har qanday tashqi xatolik yutiladi: dashboard hech qachon buzilmaydi (bo'sh/eski qiymat qaytadi).
/// </summary>
public partial class MarketDataService : IMarketDataService
{
    private static readonly HttpClient http = CreateClient();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);
    private static readonly SemaphoreSlim gate = new(1, 1);

    private static MarketDataDto? cache;
    private static DateTimeOffset cacheTime;

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
        return client;
    }

    public async Task<MarketDataDto> GetAsync(CancellationToken cancellationToken = default)
    {
        if (IsFresh())
            return cache!;

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (IsFresh())
                return cache!;

            var (usd, eur, date) = await FetchCbuAsync(cancellationToken);
            var metals = await FetchMetalsAsync(cancellationToken);

            cache = new MarketDataDto
            {
                Usd = usd,
                Eur = eur,
                RateDate = date,
                Metals = metals,
                MetalsAvailable = metals.Count > 0,
            };
            cacheTime = DateTimeOffset.UtcNow;
            return cache;
        }
        finally
        {
            gate.Release();
        }
    }

    private static bool IsFresh()
        => cache is not null && DateTimeOffset.UtcNow - cacheTime < CacheTtl;

    private static async Task<(CurrencyRateDto? usd, CurrencyRateDto? eur, string date)> FetchCbuAsync(CancellationToken ct)
    {
        try
        {
            var json = await http.GetStringAsync("https://cbu.uz/uz/arkhiv-kursov-valyut/json/", ct);
            using var doc = JsonDocument.Parse(json);

            CurrencyRateDto? usd = null, eur = null;
            var date = string.Empty;

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var ccy = el.GetProperty("Ccy").GetString();
                var rate = ParseDecimal(el.GetProperty("Rate").GetString());
                var diff = ParseDecimal(el.GetProperty("Diff").GetString());
                date = el.GetProperty("Date").GetString() ?? date;

                if (ccy == "USD") usd = new CurrencyRateDto { Rate = rate, Diff = diff };
                else if (ccy == "EUR") eur = new CurrencyRateDto { Rate = rate, Diff = diff };
            }

            return (usd, eur, date);
        }
        catch
        {
            return (null, null, string.Empty);
        }
    }

    private static async Task<IReadOnlyList<MetalPriceDto>> FetchMetalsAsync(CancellationToken ct)
    {
        (string Name, string Slug)[] defs =
        [
            ("Mis", "copper"),
            ("Alyuminiy", "aluminum"),
            ("Oltin", "gold"),
        ];

        var list = new List<MetalPriceDto>();
        foreach (var (name, slug) in defs)
        {
            var metal = await FetchOneMetalAsync(name, slug, ct);
            if (metal is not null)
                list.Add(metal);
        }

        return list;
    }

    private static async Task<MetalPriceDto?> FetchOneMetalAsync(string name, string slug, CancellationToken ct)
    {
        try
        {
            var html = await http.GetStringAsync($"https://www.investing.com/commodities/{slug}", ct);

            var price = MatchGroup(LastRegex(), html);
            if (price is null)
                return null;

            var change = MatchGroup(ChangeRegex(), html) ?? string.Empty;
            var percent = MatchGroup(PercentRegex(), html) ?? string.Empty;

            return new MetalPriceDto
            {
                Name = name,
                Price = price,
                Change = change,
                ChangePercent = percent,
                IsUp = !change.TrimStart().StartsWith('-'),
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? MatchGroup(Regex regex, string html)
    {
        var match = regex.Match(html);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static decimal ParseDecimal(string? value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0m;

    [GeneratedRegex("data-test=\"instrument-price-last\">([^<]+)<")]
    private static partial Regex LastRegex();

    [GeneratedRegex("data-test=\"instrument-price-change\">([^<]+)<")]
    private static partial Regex ChangeRegex();

    [GeneratedRegex("data-test=\"instrument-price-change-percent\">([^<]+)<")]
    private static partial Regex PercentRegex();
}
