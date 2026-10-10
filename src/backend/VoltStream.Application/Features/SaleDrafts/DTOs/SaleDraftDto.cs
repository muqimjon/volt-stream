namespace VoltStream.Application.Features.SaleDrafts.DTOs;

using VoltStream.Domain.Enums;

public record SaleDraftDto
{
    public long Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public SaleDraftStatus Status { get; set; }
    public long BranchId { get; set; }
    public string Branch { get; set; } = string.Empty;
    public long? CustomerId { get; set; }
    public string? Customer { get; set; }
    public long? ConfirmedSaleId { get; set; }
    public long? CreatedByUserId { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int TotalRolls { get; set; }
    public decimal TotalLength { get; set; }
    public IReadOnlyList<SaleDraftItemDto> Items { get; set; } = [];
}

public record SaleDraftItemDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Product { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal LengthPerRoll { get; set; }
    public int RollCount { get; set; }
    public decimal TotalLength { get; set; }
    public string Composition { get; set; } = string.Empty;
}
