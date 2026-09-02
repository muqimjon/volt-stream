namespace VoltStream.Application.Features.Monitoring.Commands;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;
using VoltStream.Application.Commons.Security;

[RequirePermission(Permissions.SettingsUsers)]
public record DeleteAllowedClientCommand(long Id) : IRequest<bool>;

public class DeleteAllowedClientCommandHandler(
    IAppDbContext context)
    : IRequestHandler<DeleteAllowedClientCommand, bool>
{
    public async Task<bool> Handle(DeleteAllowedClientCommand request, CancellationToken cancellationToken)
    {
        var client = await context.AllowedClients.FirstOrDefaultAsync(wh => wh.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AllowedClient), nameof(request.Id), request.Id);

        client.IsDeleted = true;
        return await context.SaveAsync(cancellationToken) > 0;
    }
}
