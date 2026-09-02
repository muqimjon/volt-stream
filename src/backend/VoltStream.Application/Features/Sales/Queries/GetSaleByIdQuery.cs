namespace VoltStream.Application.Features.Sales.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Domain.Entities;

public record GetSaleByIdQuery(long Id) : IRequest<SaleDto>;

public class GetSaleByIdQueryHandler(
    IAppDbContext context,
    ICurrentUser user,
    IMapper mapper)
    : IRequestHandler<GetSaleByIdQuery, SaleDto>
{
    public async Task<SaleDto> Handle(GetSaleByIdQuery request, CancellationToken cancellationToken)
        => user.Apply(mapper.Map<SaleDto>(await context.Sales
            .Include(sale => sale.Items)
            .Include(s => s.CustomerOperation)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken))
        ?? throw new NotFoundException(nameof(Sale), nameof(request.Id), request.Id));
}
