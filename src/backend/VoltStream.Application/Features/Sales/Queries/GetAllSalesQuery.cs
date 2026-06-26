namespace VoltStream.Application.Features.Sales.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Sales.DTOs;

public record GetAllSalesQuery : IRequest<IReadOnlyCollection<SaleDto>>;

public class GetAllSalesQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllSalesQuery, IReadOnlyCollection<SaleDto>>
{
    public async Task<IReadOnlyCollection<SaleDto>> Handle(GetAllSalesQuery request, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<SaleDto>>(await context.Sales
            .AsNoTracking()
            .Include(c => c.CustomerOperation)
            .ToListAsync(cancellationToken));
}
