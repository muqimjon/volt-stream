namespace ApiServices.Models.Responses;

public record WarehouseStockResponse
{
    public long Id { get; set; }
    public int RollCount { get; set; }  // rulon soni
    public decimal LengthPerRoll { get; set; }  //rulon uzunligi
    public decimal TotalLength { get; set; } // jami uzunlik
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;

    public long ProductId { get; set; }
    public ProductResponse Product { get; set; } = default!;
}