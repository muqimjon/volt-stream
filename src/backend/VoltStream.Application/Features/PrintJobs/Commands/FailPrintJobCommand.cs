namespace VoltStream.Application.Features.PrintJobs.Commands;

using MediatR;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.PrintJobs.DTOs;
using VoltStream.Domain.Enums;

[RequirePermission(Permissions.PrintAgent)]
public record FailPrintJobCommand(long Id, string Error) : IRequest<PrintJobDto>;

public class FailPrintJobCommandHandler(
    IAppDbContext context,
    IPrintJobNotifier notifier) : IRequestHandler<FailPrintJobCommand, PrintJobDto>
{
    public async Task<PrintJobDto> Handle(FailPrintJobCommand request, CancellationToken cancellationToken)
    {
        var job = (await context.FindJobAsync(request.Id, cancellationToken)).Require(PrintJobStatus.Printing);

        job.Status = PrintJobStatus.Failed;
        job.Error = request.Error;
        job.FinishedAt = DateTimeOffset.UtcNow;

        return await job.SaveAndNotifyAsync(context, notifier, cancellationToken);
    }
}
