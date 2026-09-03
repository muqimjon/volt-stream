namespace VoltStream.Application.Features.Cashes.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.PaymentsManage)]
public record UpdateCashCommand(
    long Id,
    decimal Balance,
    bool IsActive,
    long CurrencyId) : IRequest<bool>;

public class UpdateCashCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateCashCommand, bool>
{
    public async Task<bool> Handle(UpdateCashCommand request, CancellationToken cancellationToken)
    {
        var cash = await context.Cashes
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Cash), nameof(request.Id), request.Id);

        cash.Balance = request.Balance;
        cash.IsActive = request.IsActive;
        cash.CurrencyId = request.CurrencyId;

        return await context.SaveAsync(cancellationToken) > 0;
    }
}
