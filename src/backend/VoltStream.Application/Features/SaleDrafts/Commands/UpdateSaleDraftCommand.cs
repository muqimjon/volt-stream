namespace VoltStream.Application.Features.SaleDrafts.Commands;

using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Services;

public record UpdateSaleDraftCommand(
    long Id,
    DateTimeOffset Date,
    long? CustomerId,
    string Description,
    List<SaleDraftItemCommand> Items)
    : IRequest<bool>;

public class UpdateSaleDraftCommandHandler(IAppDbContext context, ICurrentUser user, BranchContext branches)
    : IRequestHandler<UpdateSaleDraftCommand, bool>
{
    public async Task<bool> Handle(UpdateSaleDraftCommand request, CancellationToken cancellationToken)
    {
        await context.BeginTransactionAsync(cancellationToken);

        try
        {
            await context.LockOpenDraftAsync(request.Id, cancellationToken);
            var draft = await context.FindDraftAsync(request.Id, user, branches, edit: true, cancellationToken);

            draft.Date = request.Date;
            draft.Description = request.Description?.Trim() ?? string.Empty;
            await draft.FillAsync(request.CustomerId, request.Items, context, cancellationToken);

            return await context.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
