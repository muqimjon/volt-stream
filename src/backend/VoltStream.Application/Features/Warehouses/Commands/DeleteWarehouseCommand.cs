namespace VoltStream.Application.Features.Warehouses.Commands;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;
using VoltStream.Application.Commons.Security;

[RequirePermission(Permissions.SettingsBranches)]
public record DeleteWarehouseCommand(long Id) : IRequest<bool>;

public class DeleteWarehouseCommandHandler(
    IAppDbContext context)
    : IRequestHandler<DeleteWarehouseCommand, bool>
{
    public async Task<bool> Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await context.Warehouses.FirstOrDefaultAsync(wh => wh.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse), nameof(request.Id), request.Id);

        warehouse.IsDeleted = true;
        return await context.SaveAsync(cancellationToken) > 0;
    }
}
