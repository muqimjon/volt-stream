namespace ApiServices.Models.Responses;

public record RollGroup
{
    public decimal Length { get; set; }
    public int Count { get; set; }
}

public record ReceiptItemResponse
{
    public string? Category { get; set; }
    public string? Product { get; set; }
    public decimal LengthPerRoll { get; set; }
    public int RollCount { get; set; }
    public decimal TotalLength { get; set; }
    public string? Composition { get; set; }
    public List<RollGroup> Rolls { get; set; } = [];
    public string? Unit { get; set; }
    public decimal Weight { get; set; }
    public decimal? WeightPerUnit { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalAmount { get; set; }
}

public record ReceiptResponse
{
    public long SaleId { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? Branch { get; set; }
    public string? Customer { get; set; }
    public string? Operator { get; set; }
    public List<ReceiptItemResponse> Items { get; set; } = [];
    public int TotalRolls { get; set; }
    public decimal TotalLength { get; set; }
    public decimal TotalWeight { get; set; }
    public bool HasPrices { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Discount { get; set; }
}
