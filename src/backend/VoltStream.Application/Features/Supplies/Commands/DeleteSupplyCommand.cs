namespace VoltStream.Application.Features.Supplies.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SuppliesEdit)]
public record DeleteSupplyCommand(long Id) : IRequest<bool>;

public class DeleteSupplyCommandHandler(
    IAppDbContext context,
    BranchContext branches)
    : IRequestHandler<DeleteSupplyCommand, bool>
{
    public async Task<bool> Handle(DeleteSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = await context.Supplies
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Supply), nameof(request.Id), request.Id);

        branches.EnsureAccess(supply.BranchId);

        var warehouseId = supply.WarehouseId ?? await branches.WarehouseOfAsync(supply.BranchId, cancellationToken);
        var stock = await context.WarehouseStocks
            .FirstOrDefaultAsync(ws => ws.ProductId == supply.ProductId &&
                                       ws.LengthPerRoll == supply.LengthPerRoll &&
                                       ws.WarehouseId == warehouseId,
                                   cancellationToken)
            ?? throw new NotFoundException(nameof(WarehouseStock), nameof(supply.Id), supply.Id);

        if (stock.TotalLength < supply.TotalLength)
            throw new ConflictException($"Omborda bu mahsulotdan faqat {stock.TotalLength} metr mavjud!");

        await context.BeginTransactionAsync(cancellationToken);

        try
        {
            stock.TotalLength -= supply.TotalLength;
            stock.RollCount -= supply.RollCount;

            context.Supplies.Remove(supply);

            return await context.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
