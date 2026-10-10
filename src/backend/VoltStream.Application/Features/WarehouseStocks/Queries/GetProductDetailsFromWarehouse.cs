namespace VoltStream.Application.Features.WarehouseStocks.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.WarehouseStocks.DTOs;

public record GetProductDetailsFromWarehouse(long Id, long? BranchId = null) : IRequest<List<WarehouseStockDto>>;

internal class GetProductDetailsFromWarehouseHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IMapper mapper) : IRequestHandler<GetProductDetailsFromWarehouse, List<WarehouseStockDto>>
{
    public async Task<List<WarehouseStockDto>> Handle(GetProductDetailsFromWarehouse request, CancellationToken cancellationToken)
    {
        var warehouseId = await branches.WarehouseFilterAsync(request.BranchId, cancellationToken);
        var query = context.WarehouseStocks
            .Include(w => w.Warehouse)
            .Where(w => !w.IsDeleted && w.ProductId == request.Id);

        if (warehouseId is not null)
            query = query.Where(w => w.WarehouseId == warehouseId);

        return user.ApplyToStocks(mapper.Map<List<WarehouseStockDto>>(await query.ToListAsync(cancellationToken)));
    }
}
