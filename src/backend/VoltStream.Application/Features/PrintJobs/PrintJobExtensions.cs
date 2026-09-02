namespace VoltStream.Application.Features.PrintJobs;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.PrintJobs.DTOs;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

public static class PrintJobExtensions
{
    public static async Task<PrintJob> FindJobAsync(this IAppDbContext context, long id, CancellationToken cancellationToken)
        => await context.PrintJobs.FirstOrDefaultAsync(job => job.Id == id, cancellationToken)
           ?? throw new NotFoundException(nameof(PrintJob), nameof(PrintJob.Id), id);

    public static PrintJob Require(this PrintJob job, params PrintJobStatus[] statuses)
        => statuses.Contains(job.Status)
            ? job
            : throw new ConflictException($"Chop etish holati mos emas: {job.Status}");

    public static async Task<PrintJobDto> SaveAndNotifyAsync(
        this PrintJob job,
        IAppDbContext context,
        IPrintJobNotifier notifier,
        CancellationToken cancellationToken)
    {
        await context.SaveAsync(cancellationToken);
        var dto = await job.ToDtoAsync(context, cancellationToken);
        await notifier.JobChanged(dto);

        return dto;
    }

    public static async Task<PrintJobDto> ToDtoAsync(this PrintJob job, IAppDbContext context, CancellationToken cancellationToken)
        => (await new[] { job }.ToDtosAsync(context, cancellationToken))[0];

    public static async Task<IReadOnlyList<PrintJobDto>> ToDtosAsync(
        this IReadOnlyCollection<PrintJob> jobs,
        IAppDbContext context,
        CancellationToken cancellationToken)
    {
        var ids = jobs.Select(job => job.RequestedByUserId).Distinct().ToList();

        var requesters = await context.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Select(user => new { user.Id, user.Username, user.PrintPolicy })
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        return [.. jobs.Select(job =>
        {
            requesters.TryGetValue(job.RequestedByUserId, out var requester);

            return new PrintJobDto
            {
                Id = job.Id,
                SaleId = job.SaleId,
                BranchId = job.BranchId,
                Status = job.Status,
                Copies = job.Copies,
                Attempts = job.Attempts,
                PrinterName = job.PrinterName,
                Error = job.Error,
                RequestedBy = requester?.Username,
                RequestedByUserId = job.RequestedByUserId,
                PrintPolicy = requester?.PrintPolicy ?? PrintPolicy.Auto,
                CreatedAt = job.CreatedAt,
                ClaimedAt = job.ClaimedAt,
                FinishedAt = job.FinishedAt
            };
        })];
    }
}
