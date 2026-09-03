namespace VoltStream.Application.Features.Cashes.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.PaymentsManage)]
public record DeleteCashCommand(long Id) : IRequest<bool>;

public class DeleteCashCommandHandler(
    IAppDbContext context)
    : IRequestHandler<DeleteCashCommand, bool>
{
    public async Task<bool> Handle(DeleteCashCommand request, CancellationToken cancellationToken)
    {
        var cash = await context.Cashes
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Cash), nameof(request.Id), request.Id);

        cash.IsDeleted = true;

        return await context.SaveAsync(cancellationToken) > 0;
    }
}
