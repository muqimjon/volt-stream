namespace VoltStream.Application.Features.WarehouseStocks.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.WarehouseStocks.DTOs;

public record WarehouseStockFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<WarehouseStockDto>>
{
    public long? BranchId { get; set; }
}

public class WarehouseStockFilterQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<WarehouseStockFilterQuery, IReadOnlyCollection<WarehouseStockDto>>
{
    public async Task<IReadOnlyCollection<WarehouseStockDto>> Handle(WarehouseStockFilterQuery request, CancellationToken cancellationToken)
    {
        user.Sanitize(request);

        var warehouseId = await branches.WarehouseFilterAsync(request.BranchId, cancellationToken);
        var query = context.WarehouseStocks.Include(ws => ws.Warehouse).AsQueryable();

        if (warehouseId is not null)
            query = query.Where(ws => ws.WarehouseId == warehouseId);

        if (!user.Has(Permissions.SalesViewPrices))
            query = query.Include(ws => ws.Product).ThenInclude(product => product.Category);

        return user.ApplyToStocks(mapper.Map<IReadOnlyCollection<WarehouseStockDto>>(await query
            .ToPagedListAsync(request, writer, cancellationToken)));
    }
}
