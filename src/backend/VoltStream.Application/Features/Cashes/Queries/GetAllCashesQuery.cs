namespace VoltStream.Application.Features.Cashes.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Cashes.DTOs;

[RequirePermission(Permissions.ReportsView)]
public record GetAllCashesQuery : IRequest<IReadOnlyCollection<CashDto>>;


public class GetAllCashesQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllCashesQuery, IReadOnlyCollection<CashDto>>
{
    public async Task<IReadOnlyCollection<CashDto>> Handle(GetAllCashesQuery request, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<CashDto>>(await context.Cashes.Where(c => !c.IsDeleted).ToListAsync(cancellationToken));
}