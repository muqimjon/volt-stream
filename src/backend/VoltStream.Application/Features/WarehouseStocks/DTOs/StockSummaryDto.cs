namespace VoltStream.Application.Features.WarehouseStocks.DTOs;

public record StockSummaryDto(long ProductId, int RollCount, decimal TotalLength);
