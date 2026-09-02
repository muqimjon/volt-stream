namespace VoltStream.Application.Features.PrintJobs.Commands;

using MediatR;
using VoltStream.Application.Commons.Interfaces;
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

        job.Status = PrintJobStatus.Printing;
        job.AgentUserId = user.UserId;
        job.PrinterName = request.PrinterName;
        job.ClaimedAt = DateTimeOffset.UtcNow;
        job.Attempts++;

        return await job.SaveAndNotifyAsync(context, notifier, cancellationToken);
    }
}
