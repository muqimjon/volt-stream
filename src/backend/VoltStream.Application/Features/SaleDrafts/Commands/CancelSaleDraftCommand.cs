namespace VoltStream.Application.Features.SaleDrafts.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.PrintJobs;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

public record CancelSaleDraftCommand(long Id) : IRequest<bool>;

public class CancelSaleDraftCommandHandler(
    IAppDbContext context,
    ICurrentUser user,
    BranchContext branches,
    IPrintJobNotifier notifier)
    : IRequestHandler<CancelSaleDraftCommand, bool>
{
    public async Task<bool> Handle(CancelSaleDraftCommand request, CancellationToken cancellationToken)
    {
        await context.FindDraftAsync(request.Id, user, branches, edit: true, cancellationToken);
        await context.BeginTransactionAsync(cancellationToken);

        List<PrintJob> jobs;

        try
        {
            var cancelled = await context.SaleDrafts
                .Where(d => d.Id == request.Id && d.Status == SaleDraftStatus.Open)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.Status, SaleDraftStatus.Cancelled)
                    .SetProperty(d => d.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

            if (cancelled == 0)
                throw new ConflictException(SaleDraftExtensions.NotOpenMessage);

            jobs = await context.PrintJobs
                .Where(job => job.SaleDraftId == request.Id
                    && (job.Status == PrintJobStatus.Pending || job.Status == PrintJobStatus.Printing))
                .ToListAsync(cancellationToken);

            foreach (var job in jobs)
            {
                job.Status = PrintJobStatus.Cancelled;
                job.FinishedAt = DateTimeOffset.UtcNow;
            }

            await context.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        foreach (var dto in await jobs.ToDtosAsync(context, cancellationToken))
            await notifier.JobChanged(dto);

        return true;
    }
}
