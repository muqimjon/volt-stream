namespace VoltStream.Application.Features.Currencies.Queries;

using MapsterMapper;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Features.Currencies.DTOs;

public record CurrencyFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<CurrencyDto>>;

public class CurrencyFilterQueryHandler(
    IAppDbContext context,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<CurrencyFilterQuery, IReadOnlyCollection<CurrencyDto>>
{
    public async Task<IReadOnlyCollection<CurrencyDto>> Handle(CurrencyFilterQuery request, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<CurrencyDto>>(await context.Currencies
            .ToPagedListAsync(request, writer, cancellationToken));
}
