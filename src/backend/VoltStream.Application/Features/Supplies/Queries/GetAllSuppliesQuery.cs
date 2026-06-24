namespace VoltStream.Application.Features.Supplies.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Supplies.DTOs;

public record GetAllSuppliesQuery : IRequest<IReadOnlyCollection<SupplyDto>>;

public class GetAllSuppliesQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllSuppliesQuery, IReadOnlyCollection<SupplyDto>>
{
    public async Task<IReadOnlyCollection<SupplyDto>> Handle(GetAllSuppliesQuery request, CancellationToken cancellationToken)
       => mapper.Map<IReadOnlyCollection<SupplyDto>>(await context.Supplies
           .AsNoTracking()
           .Where(supply => supply.IsDeleted != true)
           .Include(supply => supply.Product)
                .ThenInclude(product => product.Category)
           .ToListAsync(cancellationToken));
}
