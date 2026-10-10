namespace VoltStream.Application.Features.PrintJobs.Commands;

using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Features.PrintJobs.DTOs;
using VoltStream.Domain.Enums;

public record CancelPrintJobCommand(long Id) : IRequest<PrintJobDto>;

public class CancelPrintJobCommandHandler(
    IAppDbContext context,
    ICurrentUser user,
    IPrintJobNotifier notifier) : IRequestHandler<CancelPrintJobCommand, PrintJobDto>
{
    public async Task<PrintJobDto> Handle(CancelPrintJobCommand request, CancellationToken cancellationToken)
    {
        var job = (await context.FindJobAsync(request.Id, user, cancellationToken))
            .Require(PrintJobStatus.Pending, PrintJobStatus.Printing);

        if (!user.IsAdmin && job.RequestedByUserId != user.UserId)
            throw new ForbiddenException("Faqat so'rov egasi bekor qila oladi.");

        job.Status = PrintJobStatus.Cancelled;
        job.FinishedAt = DateTimeOffset.UtcNow;

        return await job.SaveAndNotifyAsync(context, notifier, cancellationToken);
    }
}
