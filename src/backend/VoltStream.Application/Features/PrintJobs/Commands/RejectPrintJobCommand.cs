namespace VoltStream.Application.Features.PrintJobs.Commands;

using MediatR;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.PrintJobs.DTOs;
using VoltStream.Domain.Enums;

[RequirePermission(Permissions.PrintAgent)]
public record RejectPrintJobCommand(long Id, string? Reason = null) : IRequest<PrintJobDto>;

public class RejectPrintJobCommandHandler(
    IAppDbContext context,
    IPrintJobNotifier notifier) : IRequestHandler<RejectPrintJobCommand, PrintJobDto>
{
    public async Task<PrintJobDto> Handle(RejectPrintJobCommand request, CancellationToken cancellationToken)
    {
        var job = (await context.FindJobAsync(request.Id, cancellationToken))
            .Require(PrintJobStatus.Pending, PrintJobStatus.Printing);

        job.Status = PrintJobStatus.Rejected;
        job.Error = request.Reason;
        job.FinishedAt = DateTimeOffset.UtcNow;

        return await job.SaveAndNotifyAsync(context, notifier, cancellationToken);
    }
}
