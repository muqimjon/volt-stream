namespace VoltStream.Application.Features.Currencies.Commands;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;

public record DeleteCurrencyCommand(long Id) : IRequest<bool>;

public class DeleteCurrencyCommandHandler(
    IAppDbContext context)
    : IRequestHandler<DeleteCurrencyCommand, bool>
{
    public async Task<bool> Handle(DeleteCurrencyCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Currencies.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), nameof(request.Id), request.Id);

        entity.IsDeleted = true;
        return await context.SaveAsync(cancellationToken) > 0;
    }
}
