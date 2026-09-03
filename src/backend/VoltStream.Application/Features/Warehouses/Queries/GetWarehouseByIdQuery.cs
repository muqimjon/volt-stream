namespace VoltStream.Application.Features.Warehouses.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Warehouses.DTOs;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.StockView)]
public record GetWarehouseByIdQuery(long Id) : IRequest<WarehouseDto>;

public class GetWarehouseByIdQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IMapper mapper)
    : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto>
{
    public async Task<WarehouseDto> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await context.Warehouses
            .Include(wh => wh.Stocks)
            .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse), nameof(request.Id), request.Id);

        branches.EnsureAccess(warehouse.BranchId);

        return user.Apply(mapper.Map<WarehouseDto>(warehouse));
    }
}
