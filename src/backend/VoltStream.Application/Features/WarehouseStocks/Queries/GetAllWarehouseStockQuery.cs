namespace VoltStream.Application.Features.WarehouseStocks.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.WarehouseStocks.DTOs;

public record GetAllWarehouseStockQuery() : IRequest<IReadOnlyCollection<WarehouseStockDto>>;

public class GetAllWarehouseStockQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllWarehouseStockQuery, IReadOnlyCollection<WarehouseStockDto>>
{
    public async Task<IReadOnlyCollection<WarehouseStockDto>> Handle(GetAllWarehouseStockQuery request, CancellationToken cancellationToken)
         => mapper.Map<IReadOnlyCollection<WarehouseStockDto>>(await context.WarehouseStocks
             .AsNoTracking()
             .Include(i => i.Product)
             .Where(w => w.IsDeleted != true)
             .ToListAsync(cancellationToken));

}

