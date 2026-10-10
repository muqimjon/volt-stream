namespace VoltStream.Application.Features.Monitoring.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Monitoring.DTOs;

[RequirePermission(Permissions.SettingsUsers)]
public record GetAllAllowedClientsQuery() : IRequest<IReadOnlyCollection<AllowedClientDto>>;

public class GetAllAllowedClientQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllAllowedClientsQuery, IReadOnlyCollection<AllowedClientDto>>
{
    public async Task<IReadOnlyCollection<AllowedClientDto>> Handle(GetAllAllowedClientsQuery request, CancellationToken cancellationToken)
    {
        var s = mapper.Map<IReadOnlyCollection<AllowedClientDto>>(await context.AllowedClients
                 .Where(w => !w.IsDeleted)
                 .ToListAsync(cancellationToken));

        return s;
    }
}
