namespace VoltStream.Application.Features.Sales.DTOs;

public record SaleReceiptDto
{
    public long SaleId { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Branch { get; set; } = string.Empty;
    public string? Customer { get; set; }
    public string? Operator { get; set; }
    public IReadOnlyCollection<SaleReceiptItemDto> Items { get; set; } = [];
    public int TotalRolls { get; set; }
    public decimal TotalLength { get; set; }
    public decimal TotalWeight { get; set; }
    public bool HasPrices { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Discount { get; set; }
}

public record SaleReceiptItemDto
{
    public string Category { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public decimal LengthPerRoll { get; set; }
    public int RollCount { get; set; }
    public decimal TotalLength { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal? WeightPerUnit { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalAmount { get; set; }
}
