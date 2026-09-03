namespace VoltStream.Application.Features.PrintJobs;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.PrintJobs.DTOs;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

public static class PrintJobExtensions
{
    public const int MaxAttempts = 3;

    public static async Task<PrintJob> FindJobAsync(this IAppDbContext context, long id, ICurrentUser user, CancellationToken cancellationToken)
    {
        var job = await context.PrintJobs.FirstOrDefaultAsync(job => job.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(PrintJob), nameof(PrintJob.Id), id);

        return user.Has(Permissions.BranchesAll) || job.BranchId == user.BranchId
            ? job
            : throw new ForbiddenException("Boshqa filial chop etish so'rovi.");
    }

    public static PrintJob RequireAgent(this PrintJob job, ICurrentUser user)
        => job.AgentUserId == user.UserId || user.IsAdmin
            ? job
            : throw new ForbiddenException("Faqat ishni olgan agent yakunlay oladi.");

    public static PrintJob Require(this PrintJob job, params PrintJobStatus[] statuses)
        => statuses.Contains(job.Status)
            ? job
            : throw new ConflictException($"Chop etish holati mos emas: {job.Status}");

    public static async Task<PrintJobDto> RequeueOrFailAsync(
        this PrintJob job,
        string? error,
        IAppDbContext context,
        IPrintJobNotifier notifier,
        CancellationToken cancellationToken)
    {
        var requeue = job.Attempts < MaxAttempts;
        job.Error = error;

        if (requeue)
        {
            job.Status = PrintJobStatus.Pending;
            job.AgentUserId = null;
            job.ClaimedAt = null;
            job.FinishedAt = null;
        }
        else
        {
            job.Status = PrintJobStatus.Failed;
            job.FinishedAt = DateTimeOffset.UtcNow;
        }

        await context.SaveAsync(cancellationToken);
        var dto = await job.ToDtoAsync(context, cancellationToken);
        await (requeue ? notifier.JobCreated(dto) : notifier.JobChanged(dto));

        return dto;
    }

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
