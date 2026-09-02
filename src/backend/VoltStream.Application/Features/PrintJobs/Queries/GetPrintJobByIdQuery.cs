namespace VoltStream.Application.Features.PrintJobs.Queries;

using MediatR;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.PrintJobs.DTOs;

public record GetPrintJobByIdQuery(long Id) : IRequest<PrintJobDto>;

public class GetPrintJobByIdQueryHandler(IAppDbContext context)
    : IRequestHandler<GetPrintJobByIdQuery, PrintJobDto>
{
    public async Task<PrintJobDto> Handle(GetPrintJobByIdQuery request, CancellationToken cancellationToken)
        => await (await context.FindJobAsync(request.Id, cancellationToken)).ToDtoAsync(context, cancellationToken);
}
