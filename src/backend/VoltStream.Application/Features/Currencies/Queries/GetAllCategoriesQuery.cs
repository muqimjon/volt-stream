namespace VoltStream.Application.Features.Currencies.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Features.Currencies.DTOs;

public record GetAllCurrenciesQuery : IRequest<IReadOnlyCollection<CurrencyDto>>;

public class GetAllCurrenciesQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllCurrenciesQuery, IReadOnlyCollection<CurrencyDto>>
{
    public async Task<IReadOnlyCollection<CurrencyDto>> Handle(GetAllCurrenciesQuery request, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<CurrencyDto>>(await context.Currencies
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Position)
            .ToListAsync(cancellationToken));
}