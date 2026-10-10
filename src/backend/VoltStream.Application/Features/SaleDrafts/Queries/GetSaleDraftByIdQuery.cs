namespace VoltStream.Application.Features.SaleDrafts.Queries;

using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.SaleDrafts.DTOs;

public record GetSaleDraftByIdQuery(long Id) : IRequest<SaleDraftDto>;

public class GetSaleDraftByIdQueryHandler(IAppDbContext context, ICurrentUser user, BranchContext branches)
    : IRequestHandler<GetSaleDraftByIdQuery, SaleDraftDto>
{
    public async Task<SaleDraftDto> Handle(GetSaleDraftByIdQuery request, CancellationToken cancellationToken)
    {
        var draft = await context.FindDraftAsync(request.Id, user, branches, edit: false, cancellationToken);

        return (await new[] { draft }.ToDtosAsync(context, cancellationToken))[0];
    }
}
