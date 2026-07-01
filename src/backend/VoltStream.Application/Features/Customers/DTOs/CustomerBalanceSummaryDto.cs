namespace VoltStream.Application.Features.Customers.DTOs;

public record CustomerBalanceSummaryDto
{
    public decimal Discount { get; set; }
    public decimal Debitor { get; set; }
    public decimal Creditor { get; set; }
}
