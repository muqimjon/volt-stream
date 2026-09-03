namespace ApiServices.Models.Responses;

public class SaleItemResponse
{
    public long Id { get; set; }
    public int RollCount { get; set; }
    public decimal LengthPerRoll { get; set; }
    public decimal TotalLength { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal Weight { get; set; }
    public long SaleId { get; set; }

    public long ProductId { get; set; }
    public ProductResponse? Product { get; set; }
}