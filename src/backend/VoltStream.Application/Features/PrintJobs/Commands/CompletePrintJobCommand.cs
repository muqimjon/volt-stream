namespace VoltStream.Application.Features.PrintJobs.Commands;

using MediatR;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.PrintJobs.DTOs;
using VoltStream.Domain.Enums;

[RequirePermission(Permissions.PrintAgent)]
public record CompletePrintJobCommand(long Id) : IRequest<PrintJobDto>;

public class CompletePrintJobCommandHandler(
    IAppDbContext context,
    ICurrentUser user,
    IPrintJobNotifier notifier) : IRequestHandler<CompletePrintJobCommand, PrintJobDto>
{
    public async Task<PrintJobDto> Handle(CompletePrintJobCommand request, CancellationToken cancellationToken)
    {
        var job = (await context.FindJobAsync(request.Id, user, cancellationToken))
            .Require(PrintJobStatus.Printing)
            .RequireAgent(user);

        job.Status = PrintJobStatus.Completed;
        job.Error = null;
        job.FinishedAt = DateTimeOffset.UtcNow;

        return await job.SaveAndNotifyAsync(context, notifier, cancellationToken);
    }
}
