namespace VoltStream.Application.Features.Payments.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.CustomerOperations.Commands;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.PaymentsManage)]
public record UpdatePaymentCommand(
    long Id,
    DateTimeOffset PaidAt,
    long CustomerId,
    decimal Amount,
    long CurrencyId,
    decimal ExchangeRate,
    decimal NetAmount,
    string Description)
    : IRequest<bool>;

public class UpdatePaymentCommandHandler(
    IAppDbContext context,
    IMediator mediator)
    : IRequestHandler<UpdatePaymentCommand, bool>
{
    public async Task<bool> Handle(UpdatePaymentCommand request, CancellationToken cancellationToken)
    {
        var oldPayment = await context.Payments
            .Include(p => p.CustomerOperation)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), nameof(request.Id), request.Id);

        if (oldPayment.CustomerOperation is null)
            throw new ConflictException("Tahrirlash uchun Payment yozuviga bog'langan CustomerOperation topilmadi.");

        await context.BeginTransactionAsync(cancellationToken);

        try
        {
            await mediator.Send(new DeleteCustomerOperationCommand(oldPayment.CustomerOperation.Id), cancellationToken);

            await mediator.Send(new CreatePaymentCommand(
                request.PaidAt,
                request.CustomerId,
                request.Amount,
                request.CurrencyId,
                request.ExchangeRate,
                request.NetAmount,
                request.Description), cancellationToken);

            return await context.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}