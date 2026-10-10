namespace ApiServices.Models.Responses;

public record ProductResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsWeightTracked { get; set; }
    public decimal? WeightPerUnit { get; set; }

    public long CategoryId { get; set; }
    public CategoryResponse Category { get; set; } = default!;
    public ICollection<WarehouseStockResponse> Stocks { get; set; } = default!;
}