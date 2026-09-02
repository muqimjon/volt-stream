namespace VoltStream.Application.Features.Warehouses.Commands;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;
using VoltStream.Application.Commons.Security;

[RequirePermission(Permissions.SettingsBranches)]
public record CreateWarehouseCommand(
    string Name)
    : IRequest<long>;

public class CreateWarehouseCommandHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<CreateWarehouseCommand, long>
{
    public async Task<long> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var exists = await context.Warehouses
            .AnyAsync(wh => wh.NormalizedName == request.Name.ToNormalized(), cancellationToken);

        if (exists)
            throw new AlreadyExistException(nameof(Warehouse), nameof(request.Name), request.Name);

        var warehouse = mapper.Map<Warehouse>(request);
        context.Warehouses.Add(warehouse);
        await context.SaveAsync(cancellationToken);
        return warehouse.Id;
    }
}
