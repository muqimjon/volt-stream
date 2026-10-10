namespace VoltStream.Application.Features.Monitoring.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Monitoring.DTOs;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsUsers)]
public record GetAllowedClientByIdQuery(long Id) : IRequest<AllowedClientDto>;

public class GetAllowedClientByIdQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllowedClientByIdQuery, AllowedClientDto>
{
    public async Task<AllowedClientDto> Handle(GetAllowedClientByIdQuery request, CancellationToken cancellationToken)
    {
        var client = await context.AllowedClients
            .FirstOrDefaultAsync(ac => ac.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AllowedClient), nameof(request.Id), request.Id);

        return mapper.Map<AllowedClientDto>(client);
    }
}