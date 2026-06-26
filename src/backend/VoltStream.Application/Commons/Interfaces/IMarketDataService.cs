namespace VoltStream.Application.Commons.Interfaces;

using VoltStream.Application.Features.Dashboard.DTOs;

public interface IMarketDataService
{
    Task<MarketDataDto> GetAsync(CancellationToken cancellationToken = default);
}
