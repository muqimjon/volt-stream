namespace VoltStream.Application.Features.Customers.DTOs;

public record CustomerBalanceDto
{
    public string? Customer { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public decimal Discount { get; set; }
    public decimal Debitor { get; set; }
    public decimal Creditor { get; set; }
}
