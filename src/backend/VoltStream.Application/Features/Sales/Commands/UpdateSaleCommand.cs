namespace VoltStream.Application.Features.Sales.Commands;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.CustomerOperations.Commands;
using VoltStream.Domain.Entities;

public record UpdateSaleCommand(
    long Id,
    DateTimeOffset Date,
    long? CustomerId,
    long CurrencyId,
    int RollCount,
    decimal Length,
    decimal Amount,
    bool IsDiscountApplied,
    decimal Discount,
    string Description,
    List<SaleItemCommand> Items)
    : IRequest<bool>;

public class UpdateSaleCommandHandler(
    IAppDbContext context,
    IMediator mediator)
    : IRequestHandler<UpdateSaleCommand, bool>
{
    public async Task<bool> Handle(UpdateSaleCommand request, CancellationToken cancellationToken)
    {
        var oldSale = await context.Sales
            .Include(s => s.CustomerOperation)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Sale), nameof(request.Id), request.Id);

        if (oldSale.CustomerOperation is null)
            throw new ConflictException("Tahrirlash uchun Sale yozuviga bog'langan CustomerOperation topilmadi.");

        await context.BeginTransactionAsync(cancellationToken);

        try
        {
            await mediator.Send(new DeleteCustomerOperationCommand(oldSale.CustomerOperation.Id), cancellationToken);

            await mediator.Send(new CreateSaleCommand(
                request.Date,
                request.CustomerId,
                request.CurrencyId,
                request.RollCount,
                request.Length,
                request.Amount,
                request.IsDiscountApplied,
                request.Discount,
                request.Description,
                request.Items), cancellationToken);

            return await context.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}