namespace ApiServices.Models.Responses;

public record MarketDataResponse
{
    public CurrencyRateResponse? Usd { get; set; }
    public CurrencyRateResponse? Eur { get; set; }
    public string RateDate { get; set; } = string.Empty;

    public List<MetalPriceResponse> Metals { get; set; } = [];
    public bool MetalsAvailable { get; set; }
}

public record CurrencyRateResponse
{
    public decimal Rate { get; set; }
    public decimal Diff { get; set; }
}

public record MetalPriceResponse
{
    public string Name { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Change { get; set; } = string.Empty;
    public string ChangePercent { get; set; } = string.Empty;
    public bool IsUp { get; set; }
}
