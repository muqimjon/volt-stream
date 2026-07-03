namespace VoltStream.Application.Features.Monitoring.Commands;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;

public record CreateAllowedClientCommand(
    string IpAddress,
    string? DeviceName,
    bool IsActive)
    : IRequest<long>;

public class CreateAllowedClientCommandHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<CreateAllowedClientCommand, long>
{
    public async Task<long> Handle(CreateAllowedClientCommand request, CancellationToken cancellationToken)
    {
        var exists = await context.AllowedClients
            .AnyAsync(c => c.IpAddress == request.IpAddress, cancellationToken);

        if (exists)
            throw new AlreadyExistException(nameof(AllowedClient), nameof(request.IpAddress), request.IpAddress);

        var client = mapper.Map<AllowedClient>(request);
        context.AllowedClients.Add(client);
        await context.SaveAsync(cancellationToken);
        return client.Id;
    }
}
