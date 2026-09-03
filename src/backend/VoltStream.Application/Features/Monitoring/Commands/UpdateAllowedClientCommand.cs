namespace VoltStream.Application.Features.Monitoring.Commands;

using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;
using VoltStream.Application.Commons.Security;

[RequirePermission(Permissions.SettingsUsers)]
public record UpdateAllowedClientCommand(
    long Id,
    string IpAddress,
    string? DeviceName,
    bool IsActive)
    : IRequest<bool>;

public class UpdateAllowedClientCommandHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<UpdateAllowedClientCommand, bool>
{
    public async Task<bool> Handle(UpdateAllowedClientCommand request, CancellationToken cancellationToken)
    {
        var client = await context.AllowedClients.FirstOrDefaultAsync(wh => wh.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AllowedClient), nameof(request.Id), request.Id);

        mapper.Map(request, client);
        return await context.SaveAsync(cancellationToken) > 0;
    }
}
