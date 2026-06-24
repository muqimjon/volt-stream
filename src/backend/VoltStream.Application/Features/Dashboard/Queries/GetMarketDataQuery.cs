namespace VoltStream.Application.Features.Dashboard.Queries;

using MediatR;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Dashboard.DTOs;

public record GetMarketDataQuery : IRequest<MarketDataDto>;

public class GetMarketDataQueryHandler(IMarketDataService service)
    : IRequestHandler<GetMarketDataQuery, MarketDataDto>
{
    public Task<MarketDataDto> Handle(GetMarketDataQuery request, CancellationToken cancellationToken)
        => service.GetAsync(cancellationToken);
}
