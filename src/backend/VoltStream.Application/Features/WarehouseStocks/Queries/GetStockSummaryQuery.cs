namespace VoltStream.Application.Features.WarehouseStocks.Queries;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.WarehouseStocks.DTOs;

public record GetStockSummaryQuery(long? BranchId = null) : IRequest<IReadOnlyList<StockSummaryDto>>;

public class GetStockSummaryQueryHandler(IAppDbContext context, BranchContext branches)
    : IRequestHandler<GetStockSummaryQuery, IReadOnlyList<StockSummaryDto>>
{
    public async Task<IReadOnlyList<StockSummaryDto>> Handle(GetStockSummaryQuery request, CancellationToken cancellationToken)
    {
        var warehouseId = await branches.WarehouseFilterAsync(request.BranchId, cancellationToken);
        var query = context.WarehouseStocks.Where(ws => !ws.IsDeleted && ws.RollCount > 0 && ws.TotalLength > 0 && ws.LengthPerRoll > 0);

        if (warehouseId is not null)
            query = query.Where(ws => ws.WarehouseId == warehouseId);

        return await query
            .GroupBy(ws => ws.ProductId)
            .OrderBy(group => group.Key)
            .Select(group => new StockSummaryDto(group.Key, group.Sum(ws => ws.RollCount), group.Sum(ws => ws.TotalLength)))
            .ToListAsync(cancellationToken);
    }
}
