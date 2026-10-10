namespace VoltStream.Application.Features.SaleDrafts.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Services;
using VoltStream.Domain.Enums;

public record CancelSaleDraftCommand(long Id) : IRequest<bool>;

public class CancelSaleDraftCommandHandler(IAppDbContext context, ICurrentUser user, BranchContext branches)
    : IRequestHandler<CancelSaleDraftCommand, bool>
{
    public async Task<bool> Handle(CancelSaleDraftCommand request, CancellationToken cancellationToken)
    {
        await context.FindDraftAsync(request.Id, user, branches, edit: true, cancellationToken);

        var cancelled = await context.SaleDrafts
            .Where(d => d.Id == request.Id && d.Status == SaleDraftStatus.Open)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, SaleDraftStatus.Cancelled)
                .SetProperty(d => d.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

        return cancelled > 0 ? true : throw new ConflictException(SaleDraftExtensions.NotOpenMessage);
    }
}
