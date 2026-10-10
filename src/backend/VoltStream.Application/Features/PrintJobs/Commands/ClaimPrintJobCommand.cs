namespace VoltStream.Application.Features.PrintJobs.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.PrintJobs.DTOs;
using VoltStream.Domain.Enums;

[RequirePermission(Permissions.PrintAgent)]
public record ClaimPrintJobCommand(long Id, string? PrinterName = null) : IRequest<PrintJobDto>;

public class ClaimPrintJobCommandHandler(
    IAppDbContext context,
    ICurrentUser user,
    IPrintJobNotifier notifier) : IRequestHandler<ClaimPrintJobCommand, PrintJobDto>
{
    public async Task<PrintJobDto> Handle(ClaimPrintJobCommand request, CancellationToken cancellationToken)
    {
        var job = (await context.FindJobAsync(request.Id, user, cancellationToken)).Require(PrintJobStatus.Pending);
        var now = DateTimeOffset.UtcNow;

        var claimed = await context.PrintJobs
            .Where(item => item.Id == job.Id && item.Status == PrintJobStatus.Pending)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(item => item.Status, PrintJobStatus.Printing)
                .SetProperty(item => item.AgentUserId, user.UserId)
                .SetProperty(item => item.PrinterName, request.PrinterName)
                .SetProperty(item => item.ClaimedAt, now)
                .SetProperty(item => item.Attempts, item => item.Attempts + 1)
                .SetProperty(item => item.UpdatedAt, now), cancellationToken);

        if (claimed == 0)
            throw new ConflictException("Chop etish so'rovi allaqachon olingan.");

        job.Status = PrintJobStatus.Printing;
        job.AgentUserId = user.UserId;
        job.PrinterName = request.PrinterName;
        job.ClaimedAt = now;
        job.UpdatedAt = now;
        job.Attempts++;

        var dto = await job.ToDtoAsync(context, cancellationToken);
        await notifier.JobChanged(dto);

        return dto;
    }
}
