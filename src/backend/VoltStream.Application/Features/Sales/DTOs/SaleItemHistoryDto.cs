namespace VoltStream.Application.Features.Sales.DTOs;

public record SaleItemHistoryDto
{
    public DateTimeOffset Date { get; set; }
    public string? Customer { get; set; }
    public string? Category { get; set; }
    public string? Name { get; set; }
    public decimal LengthPerRoll { get; set; }
    public int RollCount { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Unit { get; set; }
    public decimal TotalLength { get; set; }
}
