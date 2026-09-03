namespace VoltStream.Application.Features.Warehouses.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Warehouses.DTOs;

[RequirePermission(Permissions.StockView)]
public record GetAllWarehouseQuery : IRequest<IReadOnlyCollection<WarehouseDto>>;

public class GetAllWarehouseQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IMapper mapper) :
    IRequestHandler<GetAllWarehouseQuery, IReadOnlyCollection<WarehouseDto>>
{
    public async Task<IReadOnlyCollection<WarehouseDto>> Handle(GetAllWarehouseQuery query, CancellationToken cancellationToken)
    {
        var branchId = await branches.ResolveFilterAsync(null, true, cancellationToken);
        var warehouses = context.Warehouses.Where(w => !w.IsDeleted);

        if (branchId is not null)
            warehouses = warehouses.Where(w => w.BranchId == branchId);

        return user.ApplyToWarehouses(mapper.Map<IReadOnlyCollection<WarehouseDto>>(
            await warehouses.ToListAsync(cancellationToken)));
    }
}
