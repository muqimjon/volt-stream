namespace VoltStream.Application.Features.WarehouseStocks.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.WarehouseStocks.DTOs;

public record GetAllWarehouseStockQuery(long? BranchId = null) : IRequest<IReadOnlyCollection<WarehouseStockDto>>;

public class GetAllWarehouseStockQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IMapper mapper)
    : IRequestHandler<GetAllWarehouseStockQuery, IReadOnlyCollection<WarehouseStockDto>>
{
    public async Task<IReadOnlyCollection<WarehouseStockDto>> Handle(GetAllWarehouseStockQuery request, CancellationToken cancellationToken)
    {
        var branchId = await branches.ResolveFilterAsync(request.BranchId, true, cancellationToken);
        var query = context.WarehouseStocks
            .AsNoTracking()
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Where(w => w.IsDeleted != true);

        if (branchId is not null)
            query = query.Where(w => w.Warehouse.BranchId == branchId);

        return user.ApplyToStocks(mapper.Map<IReadOnlyCollection<WarehouseStockDto>>(await query.ToListAsync(cancellationToken)));
    }
}
