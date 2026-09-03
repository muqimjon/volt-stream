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
    ICurrentUser user,
    IPrintJobNotifier notifier) : IRequestHandler<FailPrintJobCommand, PrintJobDto>
{
    public async Task<PrintJobDto> Handle(FailPrintJobCommand request, CancellationToken cancellationToken)
    {
        var job = (await context.FindJobAsync(request.Id, user, cancellationToken))
            .Require(PrintJobStatus.Printing)
            .RequireAgent(user);

        return await job.RequeueOrFailAsync(request.Error, context, notifier, cancellationToken);
    }
}
