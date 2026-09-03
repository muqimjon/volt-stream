namespace VoltStream.Application.Features.Warehouses.Commands;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsBranches)]
public record UpdateWarehouseCommand(
    long Id,
    string Name)
    : IRequest<bool>;

public class UpdateWarehouseCommandHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<UpdateWarehouseCommand, bool>
{
    public async Task<bool> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await context.Warehouses.FirstOrDefaultAsync(wh => wh.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse), nameof(request.Id), request.Id);

        mapper.Map(request, warehouse);
        return await context.SaveAsync(cancellationToken) > 0;
    }
}
