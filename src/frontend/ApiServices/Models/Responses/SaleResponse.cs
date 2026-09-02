namespace ApiServices.Models.Responses;

public record SaleResponse
{
    public long Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public long? BranchId { get; set; }
    public int RollCount { get; set; }
    public decimal Length { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDiscountApplied { get; set; }

    public long CustomerOperationId { get; set; }
    public CustomerOperationResponse CustomerOperation { get; set; } = default!;

    public long CurrencyId { get; set; }
    public CurrencyResponse Currency { get; set; } = default!;

    public long CustomerId { get; set; }
    public CustomerResponse Customer { get; set; } = default!;

    public ICollection<SaleItemResponse> Items { get; set; } = default!;

}
