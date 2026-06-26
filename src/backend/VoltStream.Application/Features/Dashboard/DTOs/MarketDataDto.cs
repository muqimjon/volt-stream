namespace VoltStream.Application.Features.Dashboard.DTOs;

/// <summary>
/// Tashqi bozor ma'lumotlari: valyuta kurslari (CBU) va metall narxlari (investing.com).
/// Faqat o'qish/ko'rsatish uchun - biznes-mantiqqa ta'sir qilmaydi.
/// </summary>
public record MarketDataDto
{
    public CurrencyRateDto? Usd { get; init; }
    public CurrencyRateDto? Eur { get; init; }
    public string RateDate { get; init; } = string.Empty;

    public IReadOnlyList<MetalPriceDto> Metals { get; init; } = [];
    public bool MetalsAvailable { get; init; }
}

public record CurrencyRateDto
{
    public decimal Rate { get; init; }
    public decimal Diff { get; init; }
}

public record MetalPriceDto
{
    public string Name { get; init; } = string.Empty;          // "Mis", "Alyuminiy", "Oltin"
    public string Price { get; init; } = string.Empty;         // "6.0648"
    public string Change { get; init; } = string.Empty;        // "-82.53"
    public string ChangePercent { get; init; } = string.Empty; // "(-1.99%)"
    public bool IsUp { get; init; }
}
